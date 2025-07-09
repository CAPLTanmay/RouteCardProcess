using RouteCardProcess.Model.DTOs.BreakDownDto;

namespace RouteCardProcess.Interfaces
{
    public interface IBreakDownRepository
    {
        Task<BreakDownResponse> StartBreakDownAsync(BreakDownStartRequest request);
        Task<BreakDownResponse> EndBreakDownAsync(string notifNum);
        Task<IEnumerable<BreakDownRecordDto>> GetAllBreakDownsAsync();

    }
}
