using RouteCardProcess.Model.DTOs.SapSync;

namespace RouteCardProcess.Interfaces
{
    public interface ISapSyncService
    {
        Task SyncRoutingDataAsync(string orderNumber);
        Task<IEnumerable<RoutingDataResponse>> GetSelectedRoutingDataAsync(string orderNumber);
    }
}
