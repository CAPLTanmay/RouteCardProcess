using RouteCardProcess.Model.DTOs.RouteCardReport;

namespace RouteCardProcess.Interfaces
{
    public interface IRouteCardReportRepository
    {
         Task<IEnumerable<RouteCardReportModel>> GetRouteCardReportAsync(string workOrderNo);
        Task<IEnumerable<RouteCardReportDto>> GetRouteCardReportFilteredAsync(RouteCardReportFilterRequest request);
        Task<LossOrderResponseDto> GetLossOrderByIdsAsync(string? setupId, string? machiningId);


    }
}
