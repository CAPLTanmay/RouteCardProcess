using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using RouteCardProcess.Model;

namespace RouteCardProcess.Services
{
    public class KblAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly KblApiConfig _config;

        public KblAuthService(IHttpClientFactory httpClientFactory, IOptions<KblApiConfig> config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _config = config.Value;
        }

        public async Task<string> AuthenticateLoginAsync(KblLoginRequest request)
        {
            var authUrl = $"{_config.BaseUrl}/{_config.AuthEndpoint}";
            var response = await _httpClient.PostAsJsonAsync(authUrl, request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(); // "Success" or error message
        }

        public async Task<string> GetTokenAsync()
        {
            var tokenUrl = $"{_config.BaseUrl}/{_config.TokenEndpoint}";
            var payload = new KblTokenRequest();

            var response = await _httpClient.PostAsJsonAsync(tokenUrl, payload);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(); // The JWT token string
        }

        public async Task<KblEmpInfoResponse> GetEmployeeInfoAsync(string token, string empId)
        {
            var empInfoUrl = $"{_config.BaseUrl}/{_config.EmployeeInfoEndpoint}";
            var request = new KblEmpInfoRequest { EmpId = empId };

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

        public async Task<string?> EncryptPasswordAsync(string plainPassword)
        {
            var url = $"{_config.BaseUrl}/{_config.EncryptEndpoint}?mod={Uri.EscapeDataString(plainPassword)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var encrypted = await response.Content.ReadAsStringAsync();
            return encrypted.Trim('"');
        }
    }
}
