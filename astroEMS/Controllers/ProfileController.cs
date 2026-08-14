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
        private readonly IWebHostEnvironment _env;

        public ProfileController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            var employeeIdClaim = User.FindFirst("EmployeeID")?.Value;
            if (employeeIdClaim == null) return NotFound();
            int employeeId = int.Parse(employeeIdClaim);

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return NotFound();

            if (photo == null || photo.Length == 0)
            {
                TempData["Error"] = "No photo selected.";
                return RedirectToAction(nameof(Index));
            }

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            string extension = Path.GetExtension(photo.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only JPG or PNG images are allowed.";
                return RedirectToAction(nameof(Index));
            }

            if (photo.Length > 5 * 1024 * 1024) // 5MB limit
            {
                TempData["Error"] = "Photo must be under 5MB.";
                return RedirectToAction(nameof(Index));
            }

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profile_photos");
            Directory.CreateDirectory(uploadsFolder);

            string fileName = $"{employee.EmployeeNumber}_{DateTime.Now.Ticks}{extension}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            // Delete old photo file if one exists
            if (!string.IsNullOrEmpty(employee.ProfilePicture))
            {
                string oldPath = Path.Combine(_env.WebRootPath, employee.ProfilePicture.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            employee.ProfilePicture = $"/uploads/profile_photos/{fileName}";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile photo updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}