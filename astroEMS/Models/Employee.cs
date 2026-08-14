using System;
using System.ComponentModel.DataAnnotations;

namespace astroEMS.Models
{
    public class Employee
    {
        public int EmployeeID { get; set; }

        [Required]
        [StringLength(20)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? MiddleName { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? ContactNumber { get; set; }

        [StringLength(100)]
        [RegularExpression(@"^[^\s@]+@(gmail\.com)$",
    ErrorMessage = "Email must end in @gmail.com")]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [Required]
        [StringLength(50)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Position { get; set; } = string.Empty;

        [Required]
        public DateTime DateHired { get; set; }

        [Required]
        [StringLength(20)]
        public string EmploymentStatus { get; set; } = "Active";

        [Required]
        [StringLength(20)]
        public string EmploymentType { get; set; } = "Regular";

        public decimal BasicSalary { get; set; }

        [StringLength(20)]
        public string? SSSNumber { get; set; }

        [StringLength(20)]
        public string? PhilHealthNumber { get; set; }

        [StringLength(20)]
        public string? PagIBIGNumber { get; set; }

        [StringLength(20)]
        public string? TINNumber { get; set; }

        [StringLength(50)]
        public string? WorkSchedule { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public DateTime? DateUpdated { get; set; }
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [StringLength(50)]
        public string? UpdatedBy { get; set; }

        [StringLength(500)]
        public string? ProfilePicture { get; set; }
    }
}