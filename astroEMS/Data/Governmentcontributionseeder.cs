using astroEMS.Models;

namespace astroEMS.Data
{

    public static class GovernmentContributionSeeder
    {
        public static void Seed(AppDbContext context)
        {
            if (!context.Set<SSSContributionTable>().Any())
            {
                var brackets = new List<SSSContributionBracket>
                {
                    new() { MinSalary = 0, MaxSalary = 5249.99m, MonthlySalaryCredit = 5000,
                            EmployeeShare = 5000 * 0.05m, EmployerShare = 5000 * 0.10m + 10 }
                };

                decimal min = 5250;
                decimal msc = 5500;
                while (msc < 35000)
                {
                    decimal ec = msc <= 14500 ? 10 : 30;
                    brackets.Add(new SSSContributionBracket
                    {
                        MinSalary = min,
                        MaxSalary = min + 499.99m,
                        MonthlySalaryCredit = msc,
                        EmployeeShare = Math.Round(msc * 0.05m, 2),
                        EmployerShare = Math.Round(msc * 0.10m, 2) + ec
                    });
                    min += 500;
                    msc += 500;
                }

                brackets.Add(new SSSContributionBracket
                {
                    MinSalary = min,
                    MaxSalary = null,
                    MonthlySalaryCredit = 35000,
                    EmployeeShare = 35000 * 0.05m,
                    EmployerShare = 35000 * 0.10m + 30
                });

                var sssTable = new SSSContributionTable
                {
                    EffectiveDate = new DateTime(2025, 1, 1),
                    Description = "SSS 2025 Contribution Schedule (15% of MSC, 5% EE / 10% ER, + EC)",
                    IsActive = true,
                    Brackets = brackets
                };
                context.Set<SSSContributionTable>().Add(sssTable);
            }

            if (!context.Set<PhilHealthSetting>().Any())
            {
                context.Set<PhilHealthSetting>().Add(new PhilHealthSetting
                {
                    EffectiveDate = new DateTime(2025, 1, 1),
                    Description = "PhilHealth 2025 Premium Rate",
                    IsActive = true,
                    PremiumRate = 0.05m,       // 5% total, split 2.5%/2.5%
                    SalaryFloor = 10000m,
                    SalaryCeiling = 100000m
                });
            }

            if (!context.Set<PagIbigContributionTable>().Any())
            {
                var pagIbigTable = new PagIbigContributionTable
                {
                    EffectiveDate = new DateTime(2025, 1, 1),
                    Description = "Pag-IBIG 2025 Contribution Schedule",
                    IsActive = true,
                    Brackets = new List<PagIbigContributionBracket>
                    {
                        new() { MinSalary = 0,     MaxSalary = 1500,  EmployeeRate = 0.01m, EmployerRate = 0.02m, MaxCompensation = 10000 },
                        new() { MinSalary = 1500.01m, MaxSalary = null, EmployeeRate = 0.02m, EmployerRate = 0.02m, MaxCompensation = 10000 },
                    }
                };
                context.Set<PagIbigContributionTable>().Add(pagIbigTable);
            }

            if (!context.Set<WithholdingTaxBracket>().Any())
            {
                var effective = new DateTime(2023, 1, 1); // TRAIN law 2023-onward table
                context.Set<WithholdingTaxBracket>().AddRange(new List<WithholdingTaxBracket>
                {
                    new() { EffectiveDate = effective, MinAnnualIncome = 0,        MaxAnnualIncome = 250000,   BaseTax = 0,       Rate = 0m },
                    new() { EffectiveDate = effective, MinAnnualIncome = 250000,   MaxAnnualIncome = 400000,   BaseTax = 0,       Rate = 0.15m },
                    new() { EffectiveDate = effective, MinAnnualIncome = 400000,   MaxAnnualIncome = 800000,   BaseTax = 22500,   Rate = 0.20m },
                    new() { EffectiveDate = effective, MinAnnualIncome = 800000,   MaxAnnualIncome = 2000000,  BaseTax = 102500,  Rate = 0.25m },
                    new() { EffectiveDate = effective, MinAnnualIncome = 2000000,  MaxAnnualIncome = 8000000,  BaseTax = 402500,  Rate = 0.30m },
                    new() { EffectiveDate = effective, MinAnnualIncome = 8000000,  MaxAnnualIncome = null,     BaseTax = 2202500, Rate = 0.35m },
                });
            }

            context.SaveChanges();
        }
    }
}