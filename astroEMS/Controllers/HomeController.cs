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
            var today = DateTime.Today;

            var vm = new DashboardViewModel
            {
                TotalEmployees = await _context.Employees.CountAsync(),
                ActiveEmployees = await _context.Employees.CountAsync(e => e.EmploymentStatus == "Active"),
                ProbationaryEmployees = await _context.Employees.CountAsync(e => e.EmploymentType == "Probationary"),
                RegularEmployees = await _context.Employees.CountAsync(e => e.EmploymentType == "Regular"),
                ContractualEmployees = await _context.Employees.CountAsync(e => e.EmploymentType == "Contractual"),
                PendingCOERequests = await _context.COERequests.CountAsync(r => r.Status == "Pending"),

                PresentToday = await _context.Attendances.CountAsync(a => a.AttendanceDate == today && a.Status == "Present"),
                LateToday = await _context.Attendances.CountAsync(a => a.AttendanceDate == today && a.Status == "Late"),
                AbsentToday = await _context.Attendances.CountAsync(a => a.AttendanceDate == today && a.Status == "Absent"),

                DepartmentBreakdown = await _context.Employees
                    .GroupBy(e => e.Department)
                    .Select(g => new DepartmentStat { Department = g.Key, Count = g.Count() })
                    .OrderByDescending(d => d.Count)
                    .ToListAsync()
            };

            vm.Alerts = new List<string>();

            if (User.IsInRole("Admin") || User.IsInRole("HR"))
            {
                // ---- System-wide alerts for Admin/HR ----
                int missingGovIds = await _context.Employees.CountAsync(e =>
                    string.IsNullOrEmpty(e.SSSNumber) || string.IsNullOrEmpty(e.PhilHealthNumber) ||
                    string.IsNullOrEmpty(e.PagIBIGNumber) || string.IsNullOrEmpty(e.TINNumber));
                if (missingGovIds > 0)
                    vm.Alerts.Add($"{missingGovIds} employee(s) have incomplete government ID information.");

                if (vm.PendingCOERequests > 0)
                    vm.Alerts.Add($"{vm.PendingCOERequests} COE request(s) awaiting HR review.");

                var pendingPayroll = await _context.PayrollPeriods.CountAsync(p => p.Status == "PendingApproval");
                if (pendingPayroll > 0)
                    vm.Alerts.Add($"{pendingPayroll} payroll period(s) awaiting Admin approval.");
            }
            else
            {
                // ---- Personal alerts for the logged-in Employee ----
                var employeeIdClaim = User.FindFirst("EmployeeID")?.Value;
                if (employeeIdClaim != null)
                {
                    int employeeId = int.Parse(employeeIdClaim);
                    var me = await _context.Employees.FindAsync(employeeId);

                    if (me != null)
                    {
                        var missingFields = new List<string>();
                        if (string.IsNullOrEmpty(me.SSSNumber)) missingFields.Add("SSS");
                        if (string.IsNullOrEmpty(me.PhilHealthNumber)) missingFields.Add("PhilHealth");
                        if (string.IsNullOrEmpty(me.PagIBIGNumber)) missingFields.Add("Pag-IBIG");
                        if (string.IsNullOrEmpty(me.TINNumber)) missingFields.Add("TIN");

                        if (missingFields.Any())
                            vm.Alerts.Add($"Your {string.Join(", ", missingFields)} information is incomplete. Please coordinate with HR to update your records.");

                        var myPendingCOE = await _context.COERequests.CountAsync(r => r.EmployeeID == employeeId && r.Status == "Pending");
                        if (myPendingCOE > 0)
                            vm.Alerts.Add($"You have {myPendingCOE} COE request(s) pending review.");
                    }
                }
            }

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