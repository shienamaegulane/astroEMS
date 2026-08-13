using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace astroEMS.Models
{
    public class Position
    {
        [Key]
        public int PositionID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? DepartmentID { get; set; }

        [ForeignKey("DepartmentID")]
        public Department? Department { get; set; }
    }
}