using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using astroEMS.Data;
using System.Security.Claims;

namespace astroEMS.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly string[] RequiredDocumentTypes =
        {
            "Resume", 
            "SSS", 
            "PhilHealth", 
            "Pag-IBIG", 
            "BIR Form 1902", 
            "Contract"
        };

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var employeeIdClaim = User.FindFirst("EmployeeID")?.Value;
            if (employeeIdClaim == null) return NotFound();

            int employeeId = int.Parse(employeeIdClaim);

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);
            if (employee == null) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmployeeID == employeeId);

            var documents = await _context.EmployeeDocuments
                .Where(d => d.EmployeeID == employeeId)
                .ToListAsync();

            var checklist = RequiredDocumentTypes.Select(type =>
            {
                var doc = documents.FirstOrDefault(d => d.DocumentType == type);
                return new
                {
                    Type = type,
                    Uploaded = doc != null,
                    Remarks = doc?.Remarks
                };
            }).ToList();

            // ---- Attendance data ----

            var todayRecord = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeID == employeeId && a.AttendanceDate == DateTime.Today);

            var allRecords = await _context.Attendances
                .Where(a => a.EmployeeID == employeeId)
                .ToListAsync();

            int presentCount = allRecords.Count(a => a.Status == "Present");
            int lateCount = allRecords.Count(a => a.Status == "Late");
            int absentCount = allRecords.Count(a => a.Status == "Absent");
            int totalCount = presentCount + lateCount + absentCount;

            ViewBag.User = user;
            ViewBag.DocumentChecklist = checklist;
            ViewBag.TodayRecord = todayRecord;
            ViewBag.PresentCount = presentCount;
            ViewBag.LateCount = lateCount;
            ViewBag.AbsentCount = absentCount;
            ViewBag.TotalCount = totalCount;

            return View(employee);
        }
    }
}