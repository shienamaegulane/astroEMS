using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using astroEMS.Data;
using astroEMS.Models;

namespace astroEMS.Controllers
{
    [Authorize]
    public class COEController : Controller
    {
        private readonly AppDbContext _context;

        public COEController(AppDbContext context)
        {
            _context = context;
        }

        // Employee

        public async Task<IActionResult> MyRequests()
        {
            int employeeId = int.Parse(User.FindFirst("EmployeeID")!.Value);
            var requests = await _context.COERequests
                .Where(r => r.EmployeeID == employeeId)
                .OrderByDescending(r => r.DateRequested)
                .ToListAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Request(string purpose)
        {
            int employeeId = int.Parse(User.FindFirst("EmployeeID")!.Value);

            var req = new COERequest
            {
                EmployeeID = employeeId,
                Purpose = purpose,
                Status = "Pending"
            };
            _context.COERequests.Add(req);
            await _context.SaveChangesAsync();

            TempData["Success"] = "COE request submitted. HR/Admin will review it shortly.";
            return RedirectToAction(nameof(MyRequests));
        }

        // Admin/HR

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Index()
        {
            var requests = await _context.COERequests
                .Include(r => r.Employee)
                .OrderByDescending(r => r.DateRequested)
                .ToListAsync();
            return View(requests);
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.COERequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.RequestID == id);

            if (request == null) return NotFound();

            return View(request);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var req = await _context.COERequests.FindAsync(id);
            if (req != null)
            {
                req.Status = "Approved";
                req.DateProcessed = DateTime.Now;
                req.ProcessedBy = User.Identity?.Name;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var req = await _context.COERequests.FindAsync(id);
            if (req != null)
            {
                req.Status = "Rejected";
                req.DateProcessed = DateTime.Now;
                req.ProcessedBy = User.Identity?.Name;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Print(int id)
        {
            var req = await _context.COERequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.RequestID == id);

            if (req == null || req.Status != "Approved") return NotFound();

            if (User.IsInRole("Employee"))
            {
                int employeeId = int.Parse(User.FindFirst("EmployeeID")!.Value);
                if (req.EmployeeID != employeeId) return Forbid();
            }

            return View(req);
        }
    }
}