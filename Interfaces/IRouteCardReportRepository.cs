using RouteCardProcess.Model.DTOs.RouteCardReport;

namespace RouteCardProcess.Interfaces
{
    public interface IRouteCardReportRepository
    {
        Task<IEnumerable<RouteCardReportModel>> GetRouteCardReportAsync(string workOrderNo);
        Task<IEnumerable<RouteCardReportDto>> GetRouteCardReportFilteredAsync(RouteCardReportFilterRequest request);
        Task<IEnumerable<RouteCardReportDto>> GetRouteCardReportAllAsync(RouteCardReportFilterRequest request);
        Task<LossOrderResponseDto> GetLossOrderByIdsAsync(OrderReportRequestDto request);
        Task<ExceptionReportResponseDto?> GetExceptionReportAsync(OrderReportRequestDto request);
        Task<TimingInfoDto?> GetTimingInfoAsync(OrderReportRequestDto request);
        Task UpdateSetupTimesAsync(SetupUpdateDto dto);
        Task UpdateIdleTimesAsync(string operatorId, string setupId, string UpdatedOperatorId, List<IdleTimeUpdateDto> idleTimes);
        Task UpdateExceptionTimesAsync(string operatorId, string setupId, string UpdatedOperatorId, List<ExceptionTimeUpdateDto> exceptionTimes);
        Task UpdateMachiningTimesAsync(MachiningUpdateDto dto);
        Task UpdateMachiningIdleTimesAsync(string operatorId, string machiningId, string UpdatedOperatorId, List<MachiningIdleTimeUpdateDto> idleTimes);
        Task UpdateMachiningExceptionTimesAsync(string operatorId, string machiningId, string UpdatedOperatorId, List<MachiningExceptionUpdateDto> exceptionTimes);
        Task UpdateMachiningOperatorQuantitiesAsync( string machiningId, List<MachiningOperatorQtyUpdateDto> quantities);
    }
}
