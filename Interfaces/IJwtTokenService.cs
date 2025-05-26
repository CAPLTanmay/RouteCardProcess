namespace RouteCardProcess.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(string operatorId);
    }
}
