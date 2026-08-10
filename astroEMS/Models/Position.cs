using System.ComponentModel.DataAnnotations;

namespace astroEMS.Models
{
    public class Position
    {
        [Key]
        public int PositionID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}