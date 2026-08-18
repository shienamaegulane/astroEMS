namespace astroEMS.Models
{
    public class DashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int ProbationaryEmployees { get; set; }
        public int RegularEmployees { get; set; }
        public int ContractualEmployees { get; set; }
        public int PendingCOERequests { get; set; }

        public int PresentToday { get; set; }
        public int LateToday { get; set; }
        public int AbsentToday { get; set; }

        public List<DepartmentStat> DepartmentBreakdown { get; set; } = new();
        public List<string> Alerts { get; set; } = new();
    }

    public class DepartmentStat
    {
        public string Department { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}