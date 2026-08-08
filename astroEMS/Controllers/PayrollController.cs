using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using astroEMS.Data;
using astroEMS.Models;
using astroEMS.Services;

namespace astroEMS.Controllers
{
    [Authorize]
    public class PayrollController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PayrollService _payrollService;

        public PayrollController(AppDbContext context, PayrollService payrollService)
        {
            _context = context;
            _payrollService = payrollService;
        }

        // ---- Admin/HR: list all payroll periods ----
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Index()
        {
            var periods = await _context.PayrollPeriods
                .OrderByDescending(p => p.PeriodStart)
                .ToListAsync();
            return View(periods);
        }

        // ---- Admin/HR: create a new payroll period ----
        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePeriod(DateTime periodStart, DateTime periodEnd, DateTime payDate)
        {
            var period = new PayrollPeriod
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                PayDate = payDate,
                Status = "Open"
            };
            _context.PayrollPeriods.Add(period);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payroll period created.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Admin/HR: generate payslips for a period ----
        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int periodId)
        {
            var (generated, skipped) = await _payrollService.GeneratePayrollAsync(periodId);

            var period = await _context.PayrollPeriods.FindAsync(periodId);
            if (period != null)
            {
                period.Status = "Processed";
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Payroll generated: {generated} payslip(s) created, {skipped} skipped (already existed).";
            return RedirectToAction(nameof(Payslips), new { periodId });
        }

        // ---- Admin/HR: view all payslips for a period ----
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Payslips(int periodId)
        {
            var period = await _context.PayrollPeriods.FindAsync(periodId);
            if (period == null) return NotFound();

            var payslips = await _context.Payslips
                .Include(p => p.Employee)
                .Where(p => p.PeriodID == periodId)
                .OrderBy(p => p.Employee!.FirstName)
                .ToListAsync();

            ViewBag.Period = period;
            return View(payslips);
        }

        // ---- Everyone: view a single payslip (own, or any if Admin/HR) ----
        public async Task<IActionResult> View(int id)
        {
            var payslip = await _context.Payslips
                .Include(p => p.Employee)
                .Include(p => p.Period)
                .FirstOrDefaultAsync(p => p.PayslipID == id);

            if (payslip == null) return NotFound();

            if (User.IsInRole("Employee"))
            {
                int employeeId = int.Parse(User.FindFirst("EmployeeID")!.Value);
                if (payslip.EmployeeID != employeeId) return Forbid();
            }

            return View(payslip);
        }

        // ---- Employee: my own payslip history ----
        public async Task<IActionResult> MyPayslips()
        {
            int employeeId = int.Parse(User.FindFirst("EmployeeID")!.Value);

            var payslips = await _context.Payslips
                .Include(p => p.Period)
                .Where(p => p.EmployeeID == employeeId)
                .OrderByDescending(p => p.Period!.PeriodStart)
                .ToListAsync();

            return View(payslips);
        }
    }
}