using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IBreakdownCodeRepository
    {
        Task<int> AddAsync(BreakdownCodeRequest request);
        Task<int> UpdateAsync(BreakdownCodeRequest request);
        Task<IEnumerable<BreakdownCodeRequest>> GetAllAsync();
        Task<int> DeleteAsync(string breakdownCodeGroup, string breakdownCode);
    }
}
