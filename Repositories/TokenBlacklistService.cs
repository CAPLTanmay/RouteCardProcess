using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Repositories;
using System.Data;

namespace RouteCardProcess.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly SqlConnectionFactory _factory;

        public TokenBlacklistService(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        // Logout-based token revocation
        public async Task RevokeTokenAsync(string jti, DateTime expiry)
        {
            using var conn = _factory.CreateConnection();

            await conn.ExecuteAsync(
                "usp_RevokeToken",
                new { Jti = jti, ExpirationTime = expiry },
                commandType: CommandType.StoredProcedure);
        }

        // Middleware check — whether a token is revoked
        public async Task<bool> IsTokenRevokedAsync(string jti)
        {
            using var conn = _factory.CreateConnection();

            var count = await conn.ExecuteScalarAsync<int>(
                "usp_IsTokenRevoked",
                new { Jti = jti },
                commandType: CommandType.StoredProcedure);

            return count > 0;
        }

        // Rotation: revoke all tokens for a given operator
        public async Task RevokeAllTokensByOperatorIdAsync(string operatorId)
        {
            using var conn = _factory.CreateConnection();

            await conn.ExecuteAsync(
                "usp_RevokeAllTokensByOperatorId",
                new { OperatorId = operatorId },
                commandType: CommandType.StoredProcedure);
        }

        // Record a new token (for rotation tracking)
        public async Task RecordActiveTokenAsync(string operatorId, string jti, DateTime expiry)
        {
            using var conn = _factory.CreateConnection();

            await conn.ExecuteAsync(
                "usp_RecordActiveToken",
                new { OperatorId = operatorId, Jti = jti, Expiry = expiry },
                commandType: CommandType.StoredProcedure);
        }
    }
}
