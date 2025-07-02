using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface ILossOrderRepository
    {
        Task<int> AddLossOrderAsync(LossOrderRequest request);
        Task<int> UpdateLossOrderAsync(DeleteLossOrderRequest request);
        Task<IEnumerable<LossOrderRequest>> GetAllLossOrdersAsync();
        Task<int> DeleteLossOrderAsync(DeleteLossOrderRequest request);
    }
}
