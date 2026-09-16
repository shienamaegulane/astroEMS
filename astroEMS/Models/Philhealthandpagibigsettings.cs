using System;
using System.Collections.Generic;

namespace astroEMS.Models
{
    public class PhilHealthSetting
    {
        public int PhilHealthSettingID { get; set; }

        public DateTime EffectiveDate { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Total premium rate (employee + employer combined), e.g. 0.05 for 5%.</summary>
        public decimal PremiumRate { get; set; }

        public decimal SalaryFloor { get; set; }
        public decimal SalaryCeiling { get; set; }
    }

  
    public class PagIbigContributionTable
    {
        public int PagIbigContributionTableID { get; set; }

        public DateTime EffectiveDate { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<PagIbigContributionBracket> Brackets { get; set; } = new List<PagIbigContributionBracket>();
    }

    public class PagIbigContributionBracket
    {
        public int PagIbigContributionBracketID { get; set; }

        public int PagIbigContributionTableID { get; set; }
        public PagIbigContributionTable Table { get; set; } = null!;

        public decimal MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }

        public decimal EmployeeRate { get; set; }
        public decimal EmployerRate { get; set; }

        /// <summary>Salary is capped at this amount before the rate is applied.</summary>
        public decimal MaxCompensation { get; set; }
    }
}