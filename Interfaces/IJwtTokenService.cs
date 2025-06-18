namespace RouteCardProcess.Interfaces
{
    public interface IJwtTokenService
    {
        Task<string> GenerateTokenAsync(string operatorId);
    }
}
