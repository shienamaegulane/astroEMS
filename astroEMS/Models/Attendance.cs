using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace astroEMS.Models
{
    [Table("Attendances")]
    public class Attendance
    {
        public int AttendanceID { get; set; }

        [Required]
        public int EmployeeID { get; set; }

        [ForeignKey("EmployeeID")]
        public Employee? Employee { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime AttendanceDate { get; set; }

        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? LunchOut { get; set; }
        public TimeSpan? LunchIn { get; set; }
        public TimeSpan? TimeOut { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Present"; // Present, Late, Absent, Half Day

        [StringLength(255)]
        public string? Remarks { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.Now;
    }
}