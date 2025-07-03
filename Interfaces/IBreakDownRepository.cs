using RouteCardProcess.Model.DTOs.BreakDownDto;

namespace RouteCardProcess.Interfaces
{
    public interface IBreakDownRepository
    {
        Task<BreakDownResponse> StartBreakDownAsync(BreakDownStartRequest request);
        Task<bool> EndBreakDownAsync(string workCenterNo, string? operatorId = null, string? breakDownReasonCode = null);

    }
}
