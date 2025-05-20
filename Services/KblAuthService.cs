using RouteCardProcess.Model;

namespace RouteCardProcess.Services
{
    public class KblAuthService
    {
        private readonly HttpClient _httpClient;

        public KblAuthService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<string> AuthenticateLoginAsync(KblLoginRequest request)
        {
            var authUrl = "https://testwebapi.kirloskarpumps.com/Microservices/APIGateway/gateway/Auth";
            var response = await _httpClient.PostAsJsonAsync(authUrl, request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(); // "Success" or error message
        }

        public async Task<string> GetTokenAsync()
        {
            var tokenUrl = "https://testwebapi.kirloskarpumps.com/Microservices/APIGateway/gateway/tokenVali";
            var payload = new KblTokenRequest();

            var response = await _httpClient.PostAsJsonAsync(tokenUrl, payload);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(); // The JWT token string
        }

        public async Task<KblEmpInfoResponse> GetEmployeeInfoAsync(string token, string empId)
        {
            var empInfoUrl = "https://testwebapi.kirloskarpumps.com/Microservices/APIGateway/gateway/GetEmpInfo";
            var request = new KblEmpInfoRequest { EmpId = empId };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, empInfoUrl)
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            var empData = await response.Content.ReadFromJsonAsync<KblEmpInfoResponse>();
            return empData!;
        }
    }

}
