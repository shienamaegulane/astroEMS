using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace astroEMS.Models
{
    public class EmployeeDocument
    {
        [Key]
        public int DocumentID { get; set; }

        [Required]
        public int EmployeeID { get; set; }

        [ForeignKey("EmployeeID")]
        public Employee? Employee { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Remarks { get; set; }

        [StringLength(50)]
        public string? UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}