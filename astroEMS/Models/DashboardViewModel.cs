namespace astroEMS.Models
{
    public class DashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int ProbationaryEmployees { get; set; }
        public int PendingCOERequests { get; set; }
        public List<DepartmentStat> DepartmentBreakdown { get; set; } = new();
    }

    public class DepartmentStat
    {
        public string Department { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}