using RouteCardProcess.Model.DTOs.WeeklyReport;

namespace RouteCardProcess.Interfaces
{
    public interface IWeeklyReportRepository
    {
        Task<IEnumerable<WeeklyExceptionReportModel>> GetExceptionReportAsync(WeeklyReportRequestDto request);
    }
}
