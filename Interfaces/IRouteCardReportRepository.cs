using RouteCardProcess.Model.DTOs.RouteCardReport;

namespace RouteCardProcess.Interfaces
{
    public interface IRouteCardReportRepository
    {
        Task<IEnumerable<RouteCardReportModel>> GetRouteCardReportAsync(string workOrderNo);
        Task<IEnumerable<RouteCardReportDto>> GetRouteCardReportFilteredAsync(RouteCardReportFilterRequest request);
        Task<LossOrderResponseDto> GetLossOrderByIdsAsync(OrderReportRequestDto request);
        Task<ExceptionReportResponseDto?> GetExceptionReportAsync(OrderReportRequestDto request);
        Task<TimingInfoDto?> GetTimingInfoAsync(OrderReportRequestDto request);
        Task UpdateSetupTimesAsync(SetupUpdateDto dto);
        Task UpdateIdleTimesAsync(string setupId, int operatorId, List<IdleTimeUpdateDto> idleTimes);
        Task UpdateExceptionTimesAsync(string setupId, int operatorId, List<ExceptionTimeUpdateDto> exceptionTimes);
        Task UpdateMachiningTimesAsync(MachiningUpdateDto dto);
        Task UpdateMachiningIdleTimesAsync(string machiningId, int operatorId, List<MachiningIdleTimeUpdateDto> idleTimes);
        Task UpdateMachiningExceptionTimesAsync(string machiningId, int operatorId, List<MachiningExceptionUpdateDto> exceptionTimes);
        Task UpdateMachiningOperatorQuantitiesAsync(string machiningId, List<MachiningOperatorQtyUpdateDto> quantities);
    }
}
