using astroEMS.Data;
using astroEMS.Models;
using ClosedXML.Excel;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Formats.Asn1;
using System.Globalization;

namespace astroEMS.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;

        public AttendanceController(AppDbContext context)
        {
            _context = context;
        }

        // Standard shift start time used to determine "Late" status
        private static readonly TimeSpan ShiftStart = new TimeSpan(8, 0, 0);       
        private static readonly TimeSpan LateGraceCutoff = new TimeSpan(8, 15, 0);

        // GET: Today's Attendance only 
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var query = _context.Attendances.Include(a => a.Employee).Where(a => a.AttendanceDate == today);

            if (User.IsInRole("Employee"))
            {
                int employeeId = int.Parse(User.FindFirst("EmployeeID")!.Value);
                query = query.Where(a => a.EmployeeID == employeeId);
            }

            var records = await query.OrderBy(a => a.Employee!.FirstName).ToListAsync();

            ViewBag.PresentCount = records.Count(a => a.Status == "Present");
            ViewBag.LateCount = records.Count(a => a.Status == "Late");
            ViewBag.AbsentCount = records.Count(a => a.Status == "Absent");
            ViewBag.Today = today;

            return View(records);
        }

        // GET: Import page (Admin/HR only)
        [Authorize(Roles = "Admin,HR")]
        public IActionResult Import()
        {
            return View();
        }

        // POST: Import handler - supports CSV and XLSX
        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Please select a file to upload.";
                return View();
            }

            var rows = new List<(string EmployeeNumber, DateTime Date, TimeSpan? TimeIn, TimeSpan? TimeOut)>();
            string extension = Path.GetExtension(file.FileName).ToLower();

            try
            {
                if (extension == ".csv")
                {
                    using var stream = new StreamReader(file.OpenReadStream());
                    using var csv = new CsvReader(stream, CultureInfo.InvariantCulture);
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        string empNo = csv.GetField("EmployeeNumber") ?? "";
                        string dateStr = csv.GetField("Date") ?? "";
                        string timeInStr = csv.GetField("TimeIn") ?? "";
                        string timeOutStr = csv.GetField("TimeOut") ?? "";

                        if (string.IsNullOrWhiteSpace(empNo) || !DateTime.TryParse(dateStr, out DateTime rowDate))
                            continue;

                        TimeSpan? timeIn = TimeSpan.TryParse(timeInStr, out var ti) ? ti : null;
                        TimeSpan? timeOut = TimeSpan.TryParse(timeOutStr, out var to) ? to : null;

                        rows.Add((empNo, rowDate, timeIn, timeOut));
                    }
                }
                else if (extension == ".xlsx")
                {
                    using var stream = file.OpenReadStream();
                    using var workbook = new XLWorkbook(stream);
                    var worksheet = workbook.Worksheet(1);
                    var range = worksheet.RangeUsed();

                    foreach (var row in range.RowsUsed().Skip(1)) // skip header row
                    {
                        string empNo = row.Cell(1).GetString();
                        string dateStr = row.Cell(2).GetString();
                        string timeInStr = row.Cell(3).GetString();
                        string timeOutStr = row.Cell(4).GetString();

                        if (string.IsNullOrWhiteSpace(empNo) || !DateTime.TryParse(dateStr, out DateTime rowDate))
                            continue;

                        TimeSpan? timeIn = TimeSpan.TryParse(timeInStr, out var ti) ? ti : null;
                        TimeSpan? timeOut = TimeSpan.TryParse(timeOutStr, out var to) ? to : null;

                        rows.Add((empNo, rowDate, timeIn, timeOut));
                    }
                }
                else
                {
                    ViewBag.Error = "Unsupported file type. Please upload a .csv or .xlsx file.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Failed to read file: {ex.Message}";
                return View();
            }

            int imported = 0, skipped = 0;

            foreach (var row in rows)
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == row.EmployeeNumber);
                if (employee == null) { skipped++; continue; }

                // Skip if a record already exists for this employee + date (avoid duplicate imports)
                bool exists = await _context.Attendances.AnyAsync(a => a.EmployeeID == employee.EmployeeID && a.AttendanceDate == row.Date.Date);
                if (exists) { skipped++; continue; }

                string status = DetermineStatus(row.TimeIn);

                var attendance = new Attendance
                {
                    EmployeeID = employee.EmployeeID,
                    AttendanceDate = row.Date.Date,
                    TimeIn = row.TimeIn,
                    TimeOut = row.TimeOut,
                    Status = status
                };

                _context.Attendances.Add(attendance);
                imported++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Import complete: {imported} record(s) added, {skipped} skipped (duplicate or unmatched employee).";
            return RedirectToAction(nameof(Index));
        }

        private string DetermineStatus(TimeSpan? timeIn)
        {
            if (timeIn == null) return "Absent";
            if (timeIn > LateGraceCutoff) return "Late";
            return "Present";
        }
        private static readonly TimeSpan ShiftEnd = new TimeSpan(17, 0, 0); // 5:00 PM

        // GET: Attendance Summary for a payroll period (Admin/HR only)
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Summary(DateTime? startDate, DateTime? endDate)
        {
            // Default to the current 1-15 or 16-end cutoff if no dates given
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var today = DateTime.Today;
                if (today.Day <= 15)
                {
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = new DateTime(today.Year, today.Month, 15);
                }
                else
                {
                    startDate = new DateTime(today.Year, today.Month, 16);
                    endDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
                }
            }

            var records = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.AttendanceDate >= startDate.Value.Date && a.AttendanceDate <= endDate.Value.Date)
                .ToListAsync();

            var summary = records
                .GroupBy(a => a.Employee)
                .Select(g => new AttendanceSummaryRow
                {
                    EmployeeID = g.Key!.EmployeeID,
                    EmployeeName = $"{g.Key.FirstName} {g.Key.LastName}",
                    EmployeeNumber = g.Key.EmployeeNumber,
                    Department = g.Key.Department,
                    PresentCount = g.Count(a => a.Status == "Present"),
                    LateCount = g.Count(a => a.Status == "Late"),
                    AbsentCount = g.Count(a => a.Status == "Absent"),
                    TotalOTHours = g.Sum(a => CalculateOTHours(a.TimeOut))
                })
                .OrderBy(s => s.EmployeeName)
                .ToList();

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View(summary);
        }

        // GET: Day-by-day detail for one employee within a period
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Details(int employeeId, DateTime startDate, DateTime endDate)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return NotFound();

            var records = await _context.Attendances
                .Where(a => a.EmployeeID == employeeId && a.AttendanceDate >= startDate.Date && a.AttendanceDate <= endDate.Date)
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            ViewBag.Employee = employee;
            ViewBag.StartDate = startDate.ToString("MMM dd, yyyy");
            ViewBag.EndDate = endDate.ToString("MMM dd, yyyy");

            return View(records);
        }

        private decimal CalculateOTHours(TimeSpan? timeOut)
        {
            if (timeOut == null || timeOut <= ShiftEnd) return 0;
            return (decimal)(timeOut.Value - ShiftEnd).TotalHours;
        }
        // GET: Employee's own attendance record, defaults to current cutoff period, filterable
        public async Task<IActionResult> MyRecord(DateTime? startDate, DateTime? endDate)
        {
            int employeeId = int.Parse(User.FindFirst("EmployeeID")!.Value);

            if (!startDate.HasValue || !endDate.HasValue)
            {
                var today = DateTime.Today;
                if (today.Day <= 15)
                {
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = new DateTime(today.Year, today.Month, 15);
                }
                else
                {
                    startDate = new DateTime(today.Year, today.Month, 16);
                    endDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
                }
            }

            var records = await _context.Attendances
                .Where(a => a.EmployeeID == employeeId && a.AttendanceDate >= startDate.Value.Date && a.AttendanceDate <= endDate.Value.Date)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View(records);
        }
    }
}