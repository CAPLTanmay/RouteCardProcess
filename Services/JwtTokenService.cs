using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtTokenService(
            IConfiguration configuration,
            ISystemLoggerRepository systemLogger,
            ITokenBlacklistService tokenBlacklistService,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _systemLogger = systemLogger;
            _tokenBlacklistService = tokenBlacklistService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GenerateTokenAsync(string operatorId, string role)
        {
            try
            {
                var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "UNKNOWN";

                // 1️ Revoke any existing tokens for this operator (rotation on re-login)
                //await _tokenBlacklistService.RevokeAllTokensByOperatorIdAsync(operatorId);
                await _tokenBlacklistService.RevokeAllTokensByOperatorIdAsync(operatorId, ipAddress);

                // 2️ Build new JWT
                var jwtSettings = _configuration.GetSection("JwtSettings");

                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings["Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var jti = Guid.NewGuid().ToString();
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, operatorId),
                     new Claim(ClaimTypes.Role, role),
                    new Claim(JwtRegisteredClaimNames.Jti, jti),
                    new Claim(
                        JwtRegisteredClaimNames.Iat,
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64)
                };

                // Convert expiry to IST (UTC +5:30)
                //var expiryUtc = TimeZoneInfo.ConvertTimeFromUtc(
                //    DateTime.UtcNow.AddMinutes(
                //        double.TryParse(jwtSettings["DurationInMinutes"], out var mins) ? mins : 15),
                //    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                //  keep expiry in UTC
                var expiryUtc = DateTime.UtcNow.AddMinutes(
                    double.TryParse(jwtSettings["DurationInMinutes"], out var mins) ? mins : 15);


                var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: expiryUtc,
                    signingCredentials: creds);

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);

                // 3️ Store this new token in ActiveTokens table for tracking
                //await _tokenBlacklistService.RecordActiveTokenAsync(operatorId, jti, expiryUtc);
                await _tokenBlacklistService.RecordActiveTokenAsync(operatorId, jti, expiryUtc, ipAddress);
                return jwt;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("JwtTokenService", "GenerateTokenAsync", ex.ToString());
                throw new ApplicationException("Error generating JWT token.", ex);
            }
        }
    }
}
