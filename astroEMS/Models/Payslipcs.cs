using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace astroEMS.Models
{
    public class Payslip
    {
        [Key]
        public int PayslipID { get; set; }

        [Required]
        public int PeriodID { get; set; }

        [ForeignKey("PeriodID")]
        public PayrollPeriod? Period { get; set; }

        [Required]
        public int EmployeeID { get; set; }

        [ForeignKey("EmployeeID")]
        public Employee? Employee { get; set; }

        public int DaysPresent { get; set; }
        public int DaysLate { get; set; }
        public int DaysAbsent { get; set; }
        public int TotalLateMinutes { get; set; }
        public int TotalUndertimeMinutes { get; set; }
        public decimal TotalOvertimeHours { get; set; }

        public decimal BasicPay { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal GrossPay { get; set; }

        public decimal LateDeduction { get; set; }
        public decimal UndertimeDeduction { get; set; }
        public decimal SSSDeduction { get; set; }
        public decimal PhilHealthDeduction { get; set; }
        public decimal PagIBIGDeduction { get; set; }
        public decimal WithholdingTax { get; set; }
        public decimal TotalDeductions { get; set; }

        public decimal NetPay { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }
}