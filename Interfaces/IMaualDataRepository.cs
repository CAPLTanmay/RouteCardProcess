using RouteCardProcess.Model.DTOs.Manualdata;
using RouteCardProcess.Model.DTOs.ManualData;
using RouteCardProcess.Model.DTOs.RouteCardReport;
using RouteCardProcess.Model.DTOs.SapValidation;

namespace RouteCardProcess.Interfaces
{
    public interface IManualDataRepository
    {
        Task SyncManualDataAsync(MaualDataRequest request);
        Task<IEnumerable<ManualDataResponseDto>> GetManualDataAsync(GetMaualDataRequest request);
        Task<ManualDataUpdateResult> UpdateManualDataAsync(ManualDataUpdateDto request);
        Task<bool> InsertDelaysAsync(ManualSetupDelayRequest request);
        Task<bool> AddDelaysAsync(ManualMachiningDelayRequest request);
        Task<IEnumerable<RouteCardReportDto>> GetManualReportAsync(RouteCardReportFilterRequest request);
        Task<IEnumerable<RouteCardReportDto>> GetUploadedManualReportAsync(RouteCardReportFilterRequest request);
        Task<TimingInfoDto?> GetManualTimingInfo(OrderReportRequestDto request);
        Task<string> ConfirmManualOrderAsync(CombinedSAPConfirmationRequest request);
        Task<int> ManualDataForHandoverAsync(ManualHandoverRequest request);
    }
}
