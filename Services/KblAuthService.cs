using System.Net.Http.Headers;
using Azure;
using Microsoft.Extensions.Options;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Login;

namespace RouteCardProcess.Services
{
    public class KblAuthService : IKblAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly KblApiConfig _config;
        private readonly ISystemLoggerRepository _systemLogger;

        public KblAuthService(HttpClient httpClient, IOptions<KblApiConfig> config, ISystemLoggerRepository systemLogger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _systemLogger = systemLogger;
        }

        public async Task<string> AuthenticateLoginAsync(KblLoginRequest request)
        {
            var authUrl = $"{_config.BaseUrl}/{_config.AuthEndpoint}";

            try
            {
                var response = await _httpClient.PostAsJsonAsync(authUrl, request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(); // "Success" or error message
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("KblAuthService", "AuthenticateLoginAsync", ex.ToString());
                return "Error: Authentication failed";
            }
        }

        public async Task<string> GetTokenAsync()
        {
            var tokenUrl = $"{_config.BaseUrl}/{_config.TokenEndpoint}";
            var payload = new KblTokenRequest
            {
                ClientId = _config.ClientId,
                ClientSecret = _config.ClientSecret
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(tokenUrl, payload);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(); // The JWT token string
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("KblAuthService", "GetTokenAsync", ex.ToString());
                return null;
            }
        }

        public async Task<KblEmpInfoResponse> GetEmployeeInfoAsync(string token, string empId)
        {
            var empInfoUrl = $"{_config.BaseUrl}/{_config.EmployeeInfoEndpoint}";
            var request = new KblEmpInfoRequest { EmpId = empId };

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, empInfoUrl)
                {
                    Content = JsonContent.Create(request)
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var empData = await response.Content.ReadFromJsonAsync<KblEmpInfoResponse>();
                return empData!;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("KblAuthService", "GetEmployeeInfoAsync", ex.ToString());
                return null!;
            }
        }

        public async Task<string?> EncryptPasswordAsync(string plainPassword)
        {
            var url = $"{_config.BaseUrl}/{_config.EncryptEndpoint}?mod={Uri.EscapeDataString(plainPassword)}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;

                var encrypted = await response.Content.ReadAsStringAsync();
                return encrypted.Trim('"');
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("KblAuthService", "EncryptPasswordAsync", ex.ToString());
                return null;
            }
        }
    }
}
