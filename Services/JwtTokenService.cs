using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RouteCardProcess.Interfaces;

namespace RouteCardProcess.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ISystemLoggerRepository _systemLogger;

        public JwtTokenService(IConfiguration configuration, ISystemLoggerRepository systemLogger)
        {
            _configuration = configuration;
            _systemLogger = systemLogger;
        }

        public async Task<string> GenerateTokenAsync(string operatorId)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var jwtSettings = _configuration.GetSection("JwtSettings");

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, operatorId)
                    };

                    var token = new JwtSecurityToken(
                        issuer: jwtSettings["Issuer"],
                        audience: jwtSettings["Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["DurationInMinutes"])),
                        signingCredentials: creds
                    );

                    return new JwtSecurityTokenHandler().WriteToken(token);
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("JwtTokenService", "GenerateTokenAsync", ex.ToString());
                throw new ApplicationException("Error generating JWT token.", ex);
            }
        }
    }
}
