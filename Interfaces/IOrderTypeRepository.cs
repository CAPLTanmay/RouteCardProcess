using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IOrderTypeRepository
    {
        Task<int> AddOrderTypeAsync(OrderTypeRequest request);
        Task<int> UpdateOrderTypeAsync(OrderTypeRequest request);
        Task<IEnumerable<OrderTypeRequest>> GetAllOrderTypesAsync();
        Task<int> DeleteOrderTypeAsync(string plant, string orderType);
    }
}
