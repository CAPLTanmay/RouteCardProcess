using RouteCardProcess.Model.DTOs.Manualdata;
using RouteCardProcess.Model.DTOs.ManualData;
using RouteCardProcess.Model.DTOs.RouteCardReport;

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
    }
}
