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
        Task<SapResponseDto> ConfirmProductionOrderAsync(CombinedSAPConfirmationRequest request);
        Task<string> ConfirmLossOrderAsync(LossOrderSapRequest request);
        Task<(object productionResult, object lossResult)> ConfirmCombinedOrderAsync(CombinedConfirmationRequest request);
        Task<(object productionResult, object lossResult)> ConfirmProdAndLossOrderAsync(CombinedSAPConfirmationRequest request);
        Task<SAPBreakdownRequest?> PostBreakdownAsync(SAPBreakdownRequest request);
        Task<SAPBreakdownCloseRequest?> PostBreakdownCloseAsync(SAPBreakdownCloseRequest request);
        Task<List<SAPBreakdownStatusResponse>> GetBulkBreakdownStatusesAsync(List<string> notifNums);
    }
}
