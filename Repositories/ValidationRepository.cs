using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.SapValidation;

namespace RouteCardProcess.Repositories
{
    public class ValidationRepository : IValidationRepository
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ValidationRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            var username = configuration["SapSettings:Username"];
            var password = configuration["SapSettings:Password"];
            _baseUrl = configuration["SapSettings:BaseUrl"];

            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        public async Task<string> ValidateWorkCenterAsync(string workCenter)
        {
            string url = $"{_baseUrl}ZWORKCENTER_VALIDATESet(WORK_CENTER='{workCenter}')?$format=json";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Return raw JSON string (same style as ValidateOrderAsync)
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> ValidateOrderAsync(string order, string workCenter)
        {
            string url = $"{_baseUrl}ZVALIDATE_ORDERSet(ORDER='{order}',WORK_CENTER='{workCenter}')?$format=json";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Return the raw JSON string as is (exact SAP response)
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetRoutingDataAsync(string orderNumber)
        {
            // Construct URL with filtering & JSON format
            string url = $"{_baseUrl}ZROUTING_DATASet/?$filter=ORDER_NUMBER eq '{orderNumber}'&$format=json";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            // Return raw JSON string as is (don't modify the response)
            return jsonString;
        }

        public async Task<string> GetLossDataAsync()
        {
            // URL to get all loss data with JSON format
            string url = $"{_baseUrl}ZLOSS_DATASet?$format=json";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            // Return raw JSON string as is (don't modify the response)
            return jsonString;
        }

        public async Task<string> GetMaintenanceNotificationsAsync()
        {
            string url = $"{_baseUrl}ZMAINT_NOTIFSet?$format=json";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            // Return raw JSON string as is (don't modify response)
            return jsonString;
        }

        // Fetch CSRF token and cookie
        private async Task<(string csrfToken, string? cookie)> FetchCsrfTokenAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-CSRF-Token", "Fetch");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            if (!response.Headers.TryGetValues("X-CSRF-Token", out var tokenValues))
                throw new Exception("X-CSRF-Token header not found.");

            var csrfToken = tokenValues.FirstOrDefault();

            string? cookie = null;
            if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
            {
                cookie = cookieValues.FirstOrDefault()?.Split(';')[0]; // extract cookie name=value
            }

            return (csrfToken, cookie);
        }


        // Update Work Center
        public async Task<string> UpdateWorkCenterAsync(WorkCenterUpdateRequest request)
        {
            string fetchUrl = $"{_baseUrl}ZWC_UPDATESet";
            var (csrfToken, cookie) = await FetchCsrfTokenAsync(fetchUrl);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, fetchUrl);
            postRequest.Headers.Add("X-CSRF-Token", csrfToken);

            if (!string.IsNullOrEmpty(cookie))
                postRequest.Headers.Add("Cookie", cookie);

            postRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var json = JsonSerializer.Serialize(request);
            postRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(postRequest);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }


        // Confirm Production Order
        public async Task<string> ConfirmProductionOrderAsync(ProductionOrderConfirmationRequest request)
        {
            string fetchUrl = $"{_baseUrl}ZCONFIRMSet";
            var (csrfToken, cookie) = await FetchCsrfTokenAsync(fetchUrl);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, fetchUrl);
            postRequest.Headers.Add("X-CSRF-Token", csrfToken);

            if (!string.IsNullOrEmpty(cookie))
                postRequest.Headers.Add("Cookie", cookie);

            postRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var json = JsonSerializer.Serialize(request);
            postRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(postRequest);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }

}
