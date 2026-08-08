using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using astroEMS.Data;
using System.Diagnostics;
using astroEMS.Models;

namespace astroEMS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                TotalEmployees = await _context.Employees.CountAsync(),
                ActiveEmployees = await _context.Employees.CountAsync(e => e.EmploymentStatus == "Active"),
                ProbationaryEmployees = await _context.Employees.CountAsync(e => e.EmploymentType == "Probationary"),
                PendingCOERequests = await _context.COERequests.CountAsync(r => r.Status == "Pending"),

                DepartmentBreakdown = await _context.Employees
                    .GroupBy(e => e.Department)
                    .Select(g => new DepartmentStat { Department = g.Key, Count = g.Count() })
                    .OrderByDescending(d => d.Count)
                    .ToListAsync()
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}