using System;
using System.ComponentModel.DataAnnotations;

namespace astroEMS.Models
{
    public class PayrollPeriod
    {
        [Key]
        public int PeriodID { get; set; }

        [Required]
        public DateTime PeriodStart { get; set; }

        [Required]
        public DateTime PeriodEnd { get; set; }

        [Required]
        public DateTime PayDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Draft";
    }
}