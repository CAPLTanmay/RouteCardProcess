namespace RouteCardProcess.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task RevokeTokenAsync(string jti, DateTime expiry);
        Task<bool> IsTokenRevokedAsync(string jti);
        Task RevokeAllTokensByOperatorIdAsync(string operatorId);
        Task RecordActiveTokenAsync(string operatorId, string jti, DateTime expiry);
    }
}
