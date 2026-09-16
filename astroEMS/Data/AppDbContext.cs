using Microsoft.EntityFrameworkCore;
using astroEMS.Models;
using astroEMS.Services;

namespace astroEMS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<COERequest> COERequests { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
        public DbSet<PayrollPeriod> PayrollPeriods { get; set; }
        public DbSet<Payslip> Payslips { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<SSSContributionTable> SSSContributionTables => Set<SSSContributionTable>();
        public DbSet<SSSContributionBracket> SSSContributionBrackets => Set<SSSContributionBracket>();
        public DbSet<PhilHealthSetting> PhilHealthSettings => Set<PhilHealthSetting>();
        public DbSet<PagIbigContributionTable> PagIbigContributionTables => Set<PagIbigContributionTable>();
        public DbSet<PagIbigContributionBracket> PagIbigContributionBrackets => Set<PagIbigContributionBracket>();
        public DbSet<WithholdingTaxBracket> WithholdingTaxBrackets => Set<WithholdingTaxBracket>();
    }
}