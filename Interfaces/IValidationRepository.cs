using RouteCardProcess.Model.DTOs.SapValidation;

namespace RouteCardProcess.Interfaces
{
    public interface IValidationRepository
    {
        Task<string> ValidateWorkCenterAsync(string workCenter);
        Task<string> ValidateOrderAsync(string order, string workCenter);
        Task<string> GetRoutingDataAsync(string orderNumber);
        Task<string> GetLossDataAsync();
        Task<string> GetMaintenanceNotificationsAsync();
        Task<string> UpdateWorkCenterAsync(WorkCenterUpdateRequest request);
        Task<string> ConfirmProductionOrderAsync(ProductionOrderConfirmationRequest request);
    }
}
