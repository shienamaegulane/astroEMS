using astroEMS.Data;
using astroEMS.Models;
using Microsoft.EntityFrameworkCore;

namespace astroEMS.Services
{
    public class PayrollService
    {
        private readonly AppDbContext _context;
        private readonly IGovernmentContributionService _govService;

        // ---- Constants that are genuinely fixed by company policy / labor code,
        // NOT government-revisable rate tables, so they stay as code constants ----
        private const int PayrollDivisor = 22;          // working days per month
        private const int RegularHoursPerDay = 8;
        private const decimal OvertimeMultiplier = 1.25m;
        private const int PayPeriodsPerYear = 24;        // semi-monthly cutoffs

        private static readonly TimeSpan ShiftStart = new TimeSpan(8, 0, 0);   // 8:00 AM
        private static readonly TimeSpan ShiftEnd = new TimeSpan(17, 0, 0);    // 5:00 PM

        public PayrollService(AppDbContext context, IGovernmentContributionService govService)
        {
            _context = context;
            _govService = govService;
        }

        public async Task<(int generated, int skipped)> GeneratePayrollAsync(int periodId)
        {
            var period = await _context.PayrollPeriods.FindAsync(periodId);
            if (period == null) throw new Exception("Payroll period not found.");

            var employees = await _context.Employees
                .Where(e => e.EmploymentStatus == "Active")
                .ToListAsync();

            var existingEmployeeIds = await _context.Payslips
                .Where(p => p.PeriodID == periodId)
                .Select(p => p.EmployeeID)
                .ToListAsync();

            int generated = 0, skipped = 0;

            foreach (var employee in employees)
            {
                if (existingEmployeeIds.Contains(employee.EmployeeID))
                {
                    skipped++;
                    continue;
                }

                var payslip = await CalculatePayslipAsync(employee, period);
                _context.Payslips.Add(payslip);
                generated++;
            }

            await _context.SaveChangesAsync();
            return (generated, skipped);
        }

        private async Task<Payslip> CalculatePayslipAsync(Employee employee, PayrollPeriod period)
        {
            var attendanceRecords = await _context.Attendances
                .Where(a => a.EmployeeID == employee.EmployeeID
                    && a.AttendanceDate >= period.PeriodStart.Date
                    && a.AttendanceDate <= period.PeriodEnd.Date)
                .ToListAsync();

            // ---- Rates ----
            decimal dailyRate = employee.BasicSalary / PayrollDivisor;
            decimal hourlyRate = dailyRate / RegularHoursPerDay;
            decimal minuteRate = hourlyRate / 60;
            decimal otRate = hourlyRate * OvertimeMultiplier;

            // ---- Attendance tallies ----
            int daysPresent = attendanceRecords.Count(a => a.Status == "Present");
            int daysLate = attendanceRecords.Count(a => a.Status == "Late");
            int daysAbsent = attendanceRecords.Count(a => a.Status == "Absent");

            int totalLateMinutes = 0;
            int totalUndertimeMinutes = 0;
            decimal totalOvertimeHours = 0;

            foreach (var a in attendanceRecords)
            {
                if (a.TimeIn.HasValue && a.TimeIn.Value > ShiftStart)
                {
                    totalLateMinutes += (int)(a.TimeIn.Value - ShiftStart).TotalMinutes;
                }

                if (a.TimeOut.HasValue)
                {
                    if (a.TimeOut.Value < ShiftEnd)
                    {
                        totalUndertimeMinutes += (int)(ShiftEnd - a.TimeOut.Value).TotalMinutes;
                    }
                    else if (a.TimeOut.Value > ShiftEnd)
                    {
                        totalOvertimeHours += (decimal)(a.TimeOut.Value - ShiftEnd).TotalHours;
                    }
                }
            }

            decimal basicPay;

            if (employee.EmploymentType == "Contractual")
            {
                // No-work-no-pay: paid only for days actually worked
                int daysWorked = daysPresent + daysLate;
                basicPay = Math.Round(dailyRate * daysWorked, 2);
            }
            else
            {
                // Regular / Probationary: fixed semi-monthly salary, regardless of attendance
                basicPay = employee.BasicSalary / 2;
            }

            decimal overtimePay = Math.Round(totalOvertimeHours * otRate, 2);
            decimal grossPay = basicPay + overtimePay;

            // ---- Deductions ----
            decimal lateDeduction = Math.Round(totalLateMinutes * minuteRate, 2);
            decimal undertimeDeduction = Math.Round(totalUndertimeMinutes * minuteRate, 2);

            // Government contributions are looked up from effective-dated tables
            // as of the period's end date, NOT computed from formulas here.
            var sss = await _govService.CalculateSSSAsync(employee.BasicSalary, period.PeriodEnd);
            var philHealth = await _govService.CalculatePhilHealthAsync(employee.BasicSalary, period.PeriodEnd);
            var pagIbig = await _govService.CalculatePagIbigAsync(employee.BasicSalary, period.PeriodEnd);

            // Each of these is a full MONTHLY employee share; split across the
            // two semi-monthly cutoffs the same way the previous code did.
            decimal sssDeduction = Math.Round(sss.EmployeeShare / 2, 2);
            decimal philHealthDeduction = Math.Round(philHealth.EmployeeShare / 2, 2);
            decimal pagIbigDeduction = Math.Round(pagIbig.EmployeeShare / 2, 2);

            decimal annualTaxableIncome = employee.BasicSalary * 12;
            decimal annualTax = await _govService.CalculateAnnualWithholdingTaxAsync(annualTaxableIncome, period.PeriodEnd);
            decimal withholdingTax = Math.Round(annualTax / PayPeriodsPerYear, 2);

            decimal totalDeductions = lateDeduction + undertimeDeduction + sssDeduction
                + philHealthDeduction + pagIbigDeduction + withholdingTax;

            decimal netPay = grossPay - totalDeductions;

            return new Payslip
            {
                PeriodID = period.PeriodID,
                EmployeeID = employee.EmployeeID,
                DaysPresent = daysPresent,
                DaysLate = daysLate,
                DaysAbsent = daysAbsent,
                TotalLateMinutes = totalLateMinutes,
                TotalUndertimeMinutes = totalUndertimeMinutes,
                TotalOvertimeHours = Math.Round(totalOvertimeHours, 2),
                BasicPay = basicPay,
                OvertimePay = overtimePay,
                GrossPay = grossPay,
                LateDeduction = lateDeduction,
                UndertimeDeduction = undertimeDeduction,
                SSSDeduction = sssDeduction,
                PhilHealthDeduction = philHealthDeduction,
                PagIBIGDeduction = pagIbigDeduction,
                WithholdingTax = withholdingTax,
                TotalDeductions = Math.Round(totalDeductions, 2),
                NetPay = Math.Round(netPay, 2)
            };
        }
    }
}