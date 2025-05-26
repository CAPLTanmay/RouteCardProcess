namespace RouteCardProcess.Interfaces
{
    public interface IBreakDownRepository
    {
        Task<bool> StartBreakDownAsync(string workCenterNo, string operatorId, string? breakDownReasonCode = null);
        Task<bool> EndBreakDownAsync(string workCenterNo, string? operatorId = null, string? breakDownReasonCode = null);

    }
}
