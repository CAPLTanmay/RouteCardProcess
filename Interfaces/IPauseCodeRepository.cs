using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IPauseCodeRepository
    {
        Task<int> AddPauseCodeAsync(PauseCodeRequest request);
        Task<int> UpdatePauseCodeAsync(PauseCodeRequest request);
        Task<IEnumerable<PauseCodeRequest>> GetAllPauseCodesAsync();
        Task<int> DeletePauseCodeAsync(string plant, string pauseCode);

    }
}
