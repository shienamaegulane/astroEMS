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

        // ---- Admin/HR: Payslip list (flat table, all periods, filterable) ----
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string? search)
        {
            var query = _context.Payslips
                .Include(p => p.Employee)
                .Include(p => p.Period)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.Period!.PeriodStart >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(p => p.Period!.PeriodEnd <= endDate.Value.Date);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Employee!.FirstName.Contains(search) ||
                    p.Employee!.LastName.Contains(search) ||
                    p.Employee!.EmployeeNumber.Contains(search));
            }

            var payslips = await query
                .OrderByDescending(p => p.Period!.PeriodStart)
                .ThenBy(p => p.Employee!.FirstName)
                .ToListAsync();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Search = search;

            return View(payslips);
        }

        // ---- Admin/HR: Generate Payroll form ----
        [Authorize(Roles = "Admin,HR")]
        public IActionResult GeneratePayroll()
        {
            return View();
        }

        // ---- Admin/HR: create period + compute payslips, then go to breakdown screen ----
        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePayroll(DateTime periodStart, DateTime periodEnd, DateTime payDate)
        {
            bool overlapExists = await _context.PayrollPeriods.AnyAsync(p =>
      periodStart <= p.PeriodEnd && periodEnd >= p.PeriodStart);

            if (overlapExists)
            {
                TempData["Error"] = $"A payroll period overlapping {periodStart:MMM dd} - {periodEnd:MMM dd, yyyy} already exists. Please choose a different date range.";
                return RedirectToAction(nameof(GeneratePayroll));
            }
            var period = new PayrollPeriod
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                PayDate = payDate,
                Status = "Draft"
            };
            _context.PayrollPeriods.Add(period);
            await _context.SaveChangesAsync();

            await _payrollService.GeneratePayrollAsync(period.PeriodID);

            period.Status = "PendingApproval";
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Breakdown), new { periodId = period.PeriodID });
        }

        // ---- Admin/HR: breakdown/review screen for a generated period ----
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Breakdown(int periodId)
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

        // ---- Admin only: approve a pending period ----
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int periodId)
        {
            var period = await _context.PayrollPeriods.FindAsync(periodId);
            if (period != null)
            {
                period.Status = "Approved";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Payroll approved. Employees can now view their payslips.";
            }
            return RedirectToAction(nameof(Index));
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
                if (payslip.Period?.Status != "Approved") return Forbid();
            }

            return View(payslip);
        }

        // ---- Admin/HR: bulk print multiple payslips at once ----
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> BulkPrint(int[] ids)
        {
            if (ids == null || ids.Length == 0) return NotFound();

            var payslips = await _context.Payslips
                .Include(p => p.Employee)
                .Include(p => p.Period)
                .Where(p => ids.Contains(p.PayslipID))
                .ToListAsync();

            return View(payslips);
        }

        // ---- Employee: my own payslip history (approved periods only) ----
        public async Task<IActionResult> MyPayslips()
        {
            int employeeId = int.Parse(User.FindFirst("EmployeeID")!.Value);

            var payslips = await _context.Payslips
                .Include(p => p.Period)
                .Where(p => p.EmployeeID == employeeId && p.Period!.Status == "Approved")
                .OrderByDescending(p => p.Period!.PeriodStart)
                .ToListAsync();

            return View(payslips);
        }
    }
}