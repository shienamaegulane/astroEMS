using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using astroEMS.Data;

namespace astroEMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Employee Master List
        public async Task<IActionResult> EmployeeMasterList()
        {
            var employees = await _context.Employees.OrderBy(e => e.Department).ThenBy(e => e.FirstName).ToListAsync();
            return View(employees);
        }

        // Payroll Register - all payslips for a period
        public async Task<IActionResult> PayrollRegister(int? periodId)
        {
            var periods = await _context.PayrollPeriods.OrderByDescending(p => p.PeriodStart).ToListAsync();
            ViewBag.Periods = periods;

            if (periodId == null)
            {
                return View(new List<astroEMS.Models.Payslip>());
            }

            var payslips = await _context.Payslips
                .Include(p => p.Employee)
                .Where(p => p.PeriodID == periodId)
                .OrderBy(p => p.Employee!.FirstName)
                .ToListAsync();

            ViewBag.SelectedPeriodId = periodId;
            return View(payslips);
        }

        // Attendance Summary report (reuses same logic as Attendance/Summary)
        public async Task<IActionResult> AttendanceSummaryReport(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var today = DateTime.Today;
                startDate = new DateTime(today.Year, today.Month, 1);
                endDate = today;
            }

            var records = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.AttendanceDate >= startDate.Value.Date && a.AttendanceDate <= endDate.Value.Date)
                .ToListAsync();

            var summary = records
                .GroupBy(a => a.Employee)
                .Select(g => new
                {
                    Employee = g.Key,
                    Present = g.Count(a => a.Status == "Present"),
                    Late = g.Count(a => a.Status == "Late"),
                    Absent = g.Count(a => a.Status == "Absent")
                })
                .OrderBy(s => s.Employee!.FirstName)
                .ToList();

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.Summary = summary;

            return View();
        }
    }
}