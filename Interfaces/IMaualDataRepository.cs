using RouteCardProcess.Model.DTOs.Manualdata;
using RouteCardProcess.Model.DTOs.ManualData;

namespace RouteCardProcess.Interfaces
{
    public interface IManualDataRepository
    {
        Task SyncManualDataAsync(MaualDataRequest request);
        Task<IEnumerable<ManualDataResponseDto>> GetManualDataAsync(GetMaualDataRequest request);
        Task<ManualDataUpdateResult> UpdateManualDataAsync(ManualDataUpdateDto request);
        Task<bool> InsertDelaysAsync(ManualSetupDelayRequest request);
        Task<bool> AddDelaysAsync(ManualMachiningDelayRequest request);
    }
}
