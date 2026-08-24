using astroEMS.Data;
using astroEMS.Models;
using Microsoft.EntityFrameworkCore;

namespace astroEMS.Services
{
    public class PayrollService
    {
        private readonly AppDbContext _context;

        // ---- Configurable payroll constants ----
        private const int PayrollDivisor = 22;          // working days per month
        private const int RegularHoursPerDay = 8;
        private const decimal OvertimeMultiplier = 1.25m;

        private static readonly TimeSpan ShiftStart = new TimeSpan(8, 0, 0);   // 8:00 AM
        private static readonly TimeSpan ShiftEnd = new TimeSpan(17, 0, 0);    // 5:00 PM

        public PayrollService(AppDbContext context)
        {
            _context = context;
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

            decimal sssDeduction = Math.Round((employee.BasicSalary * 0.05m) / 2, 2);
            decimal philHealthDeduction = Math.Round((employee.BasicSalary * 0.025m) / 2, 2);
            decimal pagIbigDeduction = 100m / 2; // flat rate, per cutoff
            decimal withholdingTax = CalculateWithholdingTax(employee.BasicSalary);

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

  
        private decimal CalculateWithholdingTax(decimal monthlyBasicSalary)
        {
            decimal annualIncome = monthlyBasicSalary * 12;
            decimal annualTax;

            if (annualIncome <= 250000) annualTax = 0;
            else if (annualIncome <= 400000) annualTax = (annualIncome - 250000) * 0.15m;
            else if (annualIncome <= 800000) annualTax = 22500 + (annualIncome - 400000) * 0.20m;
            else if (annualIncome <= 2000000) annualTax = 102500 + (annualIncome - 800000) * 0.25m;
            else if (annualIncome <= 8000000) annualTax = 402500 + (annualIncome - 2000000) * 0.30m;
            else annualTax = 2202500 + (annualIncome - 8000000) * 0.35m;

            // annual -> semi-monthly (24 pay periods/year)
            return Math.Round(annualTax / 24, 2);
        }
    }
}