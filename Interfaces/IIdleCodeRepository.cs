using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IIdleCodeRepository
    {
        Task<int> AddIdleCodeAsync(IdleCodeRequest request);
        Task<int> UpdateIdleCodeAsync(IdleCodeRequest request);
        Task<IEnumerable<IdleCodeRequest>> GetAllIdleCodesAsync();
    }
}
