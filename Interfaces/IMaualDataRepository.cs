using RouteCardProcess.Model.DTOs.Manualdata;
using RouteCardProcess.Model.DTOs.ManualData;
using RouteCardProcess.Model.DTOs.SapSync;

namespace RouteCardProcess.Interfaces
{
    public interface IManualDataRepository
    {
        Task SyncManualDataAsync(MaualDataRequest request);
        Task<IEnumerable<ManualDataResponseDto>> GetManualDataAsync(GetMaualDataRequest request);
        Task<(bool Success, string SetupId, string MachiningId)> UpdateManualDataAsync(ManualDataUpdateDto request);

    }
}
