using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Configurations;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly SqlConnectionFactory _factory;

        public TokenBlacklistService(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        //  Logout-based token revocation
        public async Task RevokeTokenAsync(string jti, DateTime expiry)
        {
            using var conn = _factory.CreateConnection();

            string sql = @"INSERT INTO RevokedTokens (Jti, Expiry)
                           VALUES (@jti, @expiry);";

            await conn.ExecuteAsync(sql, new { jti, expiry });
        }

        // Middleware check — whether a token is revoked
        public async Task<bool> IsTokenRevokedAsync(string jti)
        {
            using var conn = _factory.CreateConnection();

            string sql = @"SELECT COUNT(1)
                           FROM RevokedTokens
                           WHERE Jti = @jti";

            var count = await conn.ExecuteScalarAsync<int>(sql, new { jti });
            return count > 0;
        }

        //  Rotation: revoke all tokens for a given operator
        public async Task RevokeAllTokensByOperatorIdAsync(string operatorId)
        {
            using var conn = _factory.CreateConnection();

            // Move all active tokens for this operator to RevokedTokens
            string sql = @"
                INSERT INTO RevokedTokens (Jti, Expiry)
                SELECT Jti, Expiry FROM ActiveTokens WHERE OperatorId = @operatorId;

                DELETE FROM ActiveTokens WHERE OperatorId = @operatorId;
            ";

            await conn.ExecuteAsync(sql, new { operatorId });
        }

        //  Record a new token (for rotation tracking)
        public async Task RecordActiveTokenAsync(string operatorId, string jti, DateTime expiry)
        {
            using var conn = _factory.CreateConnection();

            string sql = @"
                INSERT INTO ActiveTokens (OperatorId, Jti, Expiry)
                VALUES (@operatorId, @jti, @expiry);
            ";

            await conn.ExecuteAsync(sql, new { operatorId, jti, expiry });
        }
    }
}
