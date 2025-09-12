namespace RouteCardProcess.Model.DTOs.WeeklyReport
{
    public class WeeklyReportRequestDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? Department { get; set; }  // optional
    }
}
