using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace astroEMS.Models
{
    public class COERequest
    {
        [Key]
        public int RequestID { get; set; }

        [Required]
        public int EmployeeID { get; set; }

        [ForeignKey("EmployeeID")]
        public Employee? Employee { get; set; }

        [Required]
        [StringLength(255)]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime DateRequested { get; set; } = DateTime.Now;

        public DateTime? DateProcessed { get; set; }

        [StringLength(50)]
        public string? ProcessedBy { get; set; }
    }
}