using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using astroEMS.Data;
using astroEMS.Models;

namespace astroEMS.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

       
        private static readonly string[] RequiredDocumentTypes =
        {
            "Resume", "SSS", "PhilHealth", "Pag-IBIG", "BIR Form 1902", "Contract"
        };

        public DocumentsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(int? employeeId)
        {
            int targetId;

            if (employeeId.HasValue)
            {
                
                targetId = employeeId.Value;
            }
            else
            {
               
                var claim = User.FindFirst("EmployeeID")?.Value;
                if (claim == null) return NotFound();
                targetId = int.Parse(claim);
            }

            var employee = await _context.Employees.FindAsync(targetId);
            if (employee == null) return NotFound();

            var documents = await _context.EmployeeDocuments
                .Where(d => d.EmployeeID == targetId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

         
            var checklist = RequiredDocumentTypes.Select(type => new
            {
                Type = type,
                Document = documents.FirstOrDefault(d => d.DocumentType == type)
            }).ToList();

            ViewBag.Employee = employee;
            ViewBag.Checklist = checklist;
            ViewBag.AllDocumentTypes = RequiredDocumentTypes.Concat(new[] { "Other" }).ToList();

            return View(documents);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int employeeId, string documentType, string? remarks, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index), new { employeeId });
            }

            string[] allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".docx" };
            string extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only PDF, JPG, PNG, or DOCX files are allowed.";
                return RedirectToAction(nameof(Index), new { employeeId });
            }

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return NotFound();

          
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "employees", employee.EmployeeNumber);
            Directory.CreateDirectory(uploadsFolder);

         
            string safeTypeName = documentType.ToLower().Replace(" ", "_").Replace("-", "_");
            string uniqueFileName = $"{safeTypeName}{extension}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

          
            var existing = await _context.EmployeeDocuments
                .FirstOrDefaultAsync(d => d.EmployeeID == employeeId && d.DocumentType == documentType);

            if (existing != null)
            {
                existing.FileName = file.FileName;
                existing.FilePath = $"/uploads/employees/{employee.EmployeeNumber}/{uniqueFileName}";
                existing.Remarks = remarks;
                existing.UploadedBy = User.Identity?.Name;
                existing.UploadedAt = DateTime.Now;
            }
            else
            {
                var doc = new EmployeeDocument
                {
                    EmployeeID = employeeId,
                    DocumentType = documentType,
                    FileName = file.FileName,
                    FilePath = $"/uploads/employees/{employee.EmployeeNumber}/{uniqueFileName}",
                    Remarks = remarks,
                    UploadedBy = User.Identity?.Name
                };
                _context.EmployeeDocuments.Add(doc);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{documentType} document uploaded successfully.";
            return RedirectToAction(nameof(Index), new { employeeId });
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int employeeId)
        {
            var doc = await _context.EmployeeDocuments.FindAsync(id);
            if (doc != null)
            {
                string fullPath = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                _context.EmployeeDocuments.Remove(doc);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { employeeId });
        }
    }
}