using RouteCardProcess.Model.DTOs.WeeklyReport;

namespace RouteCardProcess.Interfaces
{
    public interface IWeeklyReportRepository
    {
        Task<IEnumerable<WeeklyExceptionReportModel>> GetExceptionReportAsync(WeeklyReportRequestDto request);
        Task<IEnumerable<WeeklyExceptionReportModel>> GetWithoutExceptionReportAsync(WeeklyReportRequestDto request);
        Task<IEnumerable<IdleReportModel>> GetIdelCodeReportAsync(WeeklyReportRequestDto request);
        Task<IEnumerable<AssociateReportModel>> GetAssociateCountsAsync();
    }
}
