using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IBreakdownGroupCodeRepository
    {
        Task<int> AddAsync(BreakdownGroupCodeRequest request);
        Task<int> UpdateAsync(BreakdownGroupCodeRequest request);
        Task<IEnumerable<BreakdownGroupCodeRequest>> GetAllAsync();
        Task<int> DeleteAsync(string breakdownCodeGroup);
        Task<IEnumerable<BreakdownCodeRequest>> GetByGroupAsync(string breakdownCodesByGroup);

    }
}
