using astroEMS.Data;
using astroEMS.Models;
using ClosedXML.Excel;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        private static readonly TimeSpan MorningStart = new TimeSpan(8, 0, 0);
        private static readonly TimeSpan MorningLateStart = new TimeSpan(8, 16, 0);
        private static readonly TimeSpan MorningAbsentCutoff = new TimeSpan(9, 0, 0);
        private static readonly TimeSpan AfternoonLateStart = new TimeSpan(13, 31, 0); // 1:31 PM
        private static readonly TimeSpan AfternoonAbsentCutoff = new TimeSpan(14, 0, 0); // 2:00 PM

        private static readonly TimeSpan ShiftEnd = new TimeSpan(17, 0, 0);

        private string GetMorningStatus(TimeSpan? timeIn)
        {
            if (!timeIn.HasValue || timeIn.Value >= MorningAbsentCutoff) return "Absent";
            if (timeIn.Value >= MorningLateStart) return "Late";
            return "Present";
        }

        private string GetAfternoonStatus(TimeSpan? lunchIn)
        {
            if (!lunchIn.HasValue || lunchIn.Value >= AfternoonAbsentCutoff) return "Absent";
            if (lunchIn.Value >= AfternoonLateStart) return "Late";
            return "Present";
        }
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

        // GET: Import page
        [Authorize(Roles = "Admin,HR")]
        public IActionResult Import()
        {
            return View();
        }

        // POST: Import handler
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

            // Raw scans: EmployeeNumber, Date, Time 
            var scans = new List<(string EmployeeNumber, DateTime Date, TimeSpan Time)>();
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
                        string timeStr = csv.GetField("Time") ?? "";

                        if (string.IsNullOrWhiteSpace(empNo) || !DateTime.TryParse(dateStr, out DateTime rowDate))
                            continue;
                        if (!TimeSpan.TryParse(timeStr, out TimeSpan scanTime))
                            continue;

                        scans.Add((empNo, rowDate.Date, scanTime));
                    }
                }
                else if (extension == ".xlsx")
                {
                    using var stream = file.OpenReadStream();
                    using var workbook = new XLWorkbook(stream);
                    var worksheet = workbook.Worksheet(1);
                    var range = worksheet.RangeUsed();

                    foreach (var row in range.RowsUsed().Skip(1))
                    {
                        string empNo = row.Cell(1).GetString();
                        string dateStr = row.Cell(2).GetString();
                        string timeStr = row.Cell(3).GetString();

                        if (string.IsNullOrWhiteSpace(empNo) || !DateTime.TryParse(dateStr, out DateTime rowDate))
                            continue;
                        if (!TimeSpan.TryParse(timeStr, out TimeSpan scanTime))
                            continue;

                        scans.Add((empNo, rowDate.Date, scanTime));
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

            // Group raw scans by Employee + Date, then map scans in order to TimeIn/LunchOut/LunchIn/TimeOut
            var grouped = scans
                .GroupBy(s => new { s.EmployeeNumber, s.Date })
                .ToList();

            int imported = 0, skipped = 0;

            foreach (var group in grouped)
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeNumber == group.Key.EmployeeNumber);
                if (employee == null) { skipped++; continue; }

                bool exists = await _context.Attendances.AnyAsync(a => a.EmployeeID == employee.EmployeeID && a.AttendanceDate == group.Key.Date);
                if (exists) { skipped++; continue; }

                var orderedScans = group.Select(s => s.Time).OrderBy(t => t).ToList();

                TimeSpan? timeIn = null, lunchOut = null, lunchIn = null, timeOut = null;

                if (orderedScans.Count == 1)
                {
                    timeIn = orderedScans[0];
                }
                else if (orderedScans.Count == 2)
                {
                    timeIn = orderedScans[0];
                    timeOut = orderedScans[1];
                }
                else if (orderedScans.Count == 3)
                {
                    timeIn = orderedScans[0];
                    lunchOut = orderedScans[1];
                    timeOut = orderedScans[2];
                }
                else if (orderedScans.Count >= 4)
                {
                    timeIn = orderedScans[0];
                    lunchOut = orderedScans[1];
                    lunchIn = orderedScans[2];
                    timeOut = orderedScans[orderedScans.Count - 1];
                }

                string status = DetermineStatus(timeIn,lunchIn);

                var attendance = new Attendance
                {
                    EmployeeID = employee.EmployeeID,
                    AttendanceDate = group.Key.Date,
                    TimeIn = timeIn,
                    LunchOut = lunchOut,
                    LunchIn = lunchIn,
                    TimeOut = timeOut,
                    Status = status
                };

                _context.Attendances.Add(attendance);
                imported++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Import complete: {imported} record(s) added, {skipped} skipped (duplicate or unmatched employee).";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Summary(DateTime? startDate, DateTime? endDate)
        {
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

            var query = _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.AttendanceDate >= startDate.Value.Date && a.AttendanceDate <= endDate.Value.Date);

            // HR sees only their own department; Admin sees everyone
            if (User.IsInRole("HR") && !User.IsInRole("Admin"))
            {
                int employeeId = int.Parse(User.FindFirst("EmployeeID")!.Value);
                var hrEmployee = await _context.Employees.FindAsync(employeeId);
                if (hrEmployee != null)
                {
                    query = query.Where(a => a.Employee!.Department == hrEmployee.Department);
                }
            }

            var records = await query.ToListAsync();

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

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Details(int employeeId, DateTime startDate, DateTime endDate)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return NotFound();

            // HR can only view employees in their own department
            if (User.IsInRole("HR") && !User.IsInRole("Admin"))
            {
                int hrEmployeeId = int.Parse(User.FindFirst("EmployeeID")!.Value);
                var hrEmployee = await _context.Employees.FindAsync(hrEmployeeId);
                if (hrEmployee == null || employee.Department != hrEmployee.Department)
                {
                    return Forbid();
                }
            }

            var records = await _context.Attendances
                .Where(a => a.EmployeeID == employeeId && a.AttendanceDate >= startDate.Date && a.AttendanceDate <= endDate.Date)
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            ViewBag.Employee = employee;
            ViewBag.StartDate = startDate.ToString("MMM dd, yyyy");
            ViewBag.EndDate = endDate.ToString("MMM dd, yyyy");

            return View(records);
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
        

        private decimal CalculateOTHours(TimeSpan? timeOut)
        {
            if (timeOut == null || timeOut <= ShiftEnd) return 0;
            return (decimal)(timeOut.Value - ShiftEnd).TotalHours;
        }

        private string DetermineStatus(TimeSpan? timeIn, TimeSpan? lunchIn)
        {
            string morning = GetMorningStatus(timeIn);
            string afternoon = GetAfternoonStatus(lunchIn);

            if (morning == "Absent" && afternoon == "Absent") return "Absent";
            if (morning == "Present" && afternoon == "Present") return "Present";
            if (morning == "Absent" || afternoon == "Absent") return "Half Day";
            return "Late"; 
        }
    }
}