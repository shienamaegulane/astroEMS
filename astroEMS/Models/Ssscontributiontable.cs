using System;
using System.Collections.Generic;

namespace astroEMS.Models
{
   
    public class SSSContributionTable
    {
        public int SSSContributionTableID { get; set; }

        public DateTime EffectiveDate { get; set; }

       
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<SSSContributionBracket> Brackets { get; set; } = new List<SSSContributionBracket>();
    }

   
    public class SSSContributionBracket
    {
        public int SSSContributionBracketID { get; set; }
        public int SSSContributionTableID { get; set; }
        public SSSContributionTable Table { get; set; } = null!;
        public decimal MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public decimal MonthlySalaryCredit { get; set; }
        public decimal EmployeeShare { get; set; }
        public decimal EmployerShare { get; set; }
    }
}