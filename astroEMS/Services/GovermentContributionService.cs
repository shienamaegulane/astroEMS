using astroEMS.Data;
using astroEMS.Models;
using Microsoft.EntityFrameworkCore;

namespace astroEMS.Services
{
    public record ContributionResult(decimal EmployeeShare, decimal EmployerShare);


    public interface IGovernmentContributionService
    {
        Task<ContributionResult> CalculateSSSAsync(decimal monthlyBasicSalary, DateTime asOfDate);
        Task<ContributionResult> CalculatePhilHealthAsync(decimal monthlyBasicSalary, DateTime asOfDate);
        Task<ContributionResult> CalculatePagIbigAsync(decimal monthlyBasicSalary, DateTime asOfDate);

        /// <summary>Returns the ANNUAL withholding tax. Caller decides how to prorate per payroll period.</summary>
        Task<decimal> CalculateAnnualWithholdingTaxAsync(decimal annualTaxableIncome, DateTime asOfDate);
    }

    public class GovernmentContributionService : IGovernmentContributionService
    {
        private readonly AppDbContext _context;

        public GovernmentContributionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ContributionResult> CalculateSSSAsync(decimal monthlyBasicSalary, DateTime asOfDate)
        {
            var table = await _context.Set<SSSContributionTable>()
                .Where(t => t.IsActive && t.EffectiveDate <= asOfDate)
                .OrderByDescending(t => t.EffectiveDate)
                .Include(t => t.Brackets)
                .FirstOrDefaultAsync();

            if (table == null)
                throw new InvalidOperationException(
                    $"No active SSS contribution table found effective on or before {asOfDate:yyyy-MM-dd}.");

            var bracket = table.Brackets
                .Where(b => monthlyBasicSalary >= b.MinSalary
                    && (b.MaxSalary == null || monthlyBasicSalary <= b.MaxSalary))
                .OrderByDescending(b => b.MinSalary)
                .FirstOrDefault();

            if (bracket == null)
                throw new InvalidOperationException(
                    $"No SSS bracket matches salary {monthlyBasicSalary} in table effective {table.EffectiveDate:yyyy-MM-dd}.");

            return new ContributionResult(bracket.EmployeeShare, bracket.EmployerShare);
        }

        public async Task<ContributionResult> CalculatePhilHealthAsync(decimal monthlyBasicSalary, DateTime asOfDate)
        {
            var setting = await _context.Set<PhilHealthSetting>()
                .Where(s => s.IsActive && s.EffectiveDate <= asOfDate)
                .OrderByDescending(s => s.EffectiveDate)
                .FirstOrDefaultAsync();

            if (setting == null)
                throw new InvalidOperationException(
                    $"No active PhilHealth setting found effective on or before {asOfDate:yyyy-MM-dd}.");

            decimal baseSalary = Math.Clamp(monthlyBasicSalary, setting.SalaryFloor, setting.SalaryCeiling);
            decimal totalPremium = Math.Round(baseSalary * setting.PremiumRate, 2);
            decimal share = Math.Round(totalPremium / 2, 2);

            return new ContributionResult(share, share);
        }

        public async Task<ContributionResult> CalculatePagIbigAsync(decimal monthlyBasicSalary, DateTime asOfDate)
        {
            var table = await _context.Set<PagIbigContributionTable>()
                .Where(t => t.IsActive && t.EffectiveDate <= asOfDate)
                .OrderByDescending(t => t.EffectiveDate)
                .Include(t => t.Brackets)
                .FirstOrDefaultAsync();

            if (table == null)
                throw new InvalidOperationException(
                    $"No active Pag-IBIG contribution table found effective on or before {asOfDate:yyyy-MM-dd}.");

            var bracket = table.Brackets
                .Where(b => monthlyBasicSalary >= b.MinSalary
                    && (b.MaxSalary == null || monthlyBasicSalary <= b.MaxSalary))
                .OrderByDescending(b => b.MinSalary)
                .FirstOrDefault();

            if (bracket == null)
                throw new InvalidOperationException(
                    $"No Pag-IBIG bracket matches salary {monthlyBasicSalary} in table effective {table.EffectiveDate:yyyy-MM-dd}.");

            decimal compensation = Math.Min(monthlyBasicSalary, bracket.MaxCompensation);
            decimal employeeShare = Math.Round(compensation * bracket.EmployeeRate, 2);
            decimal employerShare = Math.Round(compensation * bracket.EmployerRate, 2);

            return new ContributionResult(employeeShare, employerShare);
        }

        public async Task<decimal> CalculateAnnualWithholdingTaxAsync(decimal annualTaxableIncome, DateTime asOfDate)
        {
            var bracket = await _context.Set<WithholdingTaxBracket>()
                .Where(b => b.IsActive && b.EffectiveDate <= asOfDate
                    && annualTaxableIncome >= b.MinAnnualIncome
                    && (b.MaxAnnualIncome == null || annualTaxableIncome <= b.MaxAnnualIncome))
                .OrderByDescending(b => b.EffectiveDate)
                .FirstOrDefaultAsync();

            if (bracket == null)
                throw new InvalidOperationException(
                    $"No withholding tax bracket matches annual income {annualTaxableIncome} as of {asOfDate:yyyy-MM-dd}.");

            decimal excess = annualTaxableIncome - bracket.MinAnnualIncome;
            return Math.Round(bracket.BaseTax + excess * bracket.Rate, 2);
        }
    }
}