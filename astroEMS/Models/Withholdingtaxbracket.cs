using System;

namespace astroEMS.Models
{
    
    public class WithholdingTaxBracket
    {
        public int WithholdingTaxBracketID { get; set; }

        public DateTime EffectiveDate { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public decimal MinAnnualIncome { get; set; }

        /// <summary>Null = open-ended top bracket.</summary>
        public decimal? MaxAnnualIncome { get; set; }

        /// <summary>Fixed base tax for this bracket.</summary>
        public decimal BaseTax { get; set; }
        public decimal Rate { get; set; }
    }
}