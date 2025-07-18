using System.Data;
using Dapper;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.RouteCardReport;
using RouteCardProcess.Model.DTOs.SapValidation;
using System.Transactions;

namespace RouteCardProcess.Repositories
{
    public class ValidationRepository : IValidationRepository
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly SqlConnectionFactory _connectionFactory;

        public ValidationRepository(HttpClient httpClient, IConfiguration configuration, ISystemLoggerRepository systemLogger, SqlConnectionFactory connectionFactory)
        {
            _httpClient = httpClient;

            var username = configuration["SapSettings:Username"];
            var password = configuration["SapSettings:Password"];
            _baseUrl = configuration["SapSettings:BaseUrl"];

            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            _systemLogger = systemLogger;
            _connectionFactory = connectionFactory;
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
        public async Task<string> ConfirmProductionOrderAsync(CombinedSAPConfirmationRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();


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

            var responseData = await response.Content.ReadAsStringAsync();

            //  Update DB flags only if success
            var sql = @"UPDATE Trans_Setup SET IsUploadToSAP = 1 WHERE SetupId = @SetupId;
            UPDATE Trans_Machining_Operator SET IsUploadToSAP = 1 WHERE MachiningId = @MachiningId; ";
            await connection.ExecuteAsync(sql, new
            {
                SetupId = request.LossOrder.SetupId,
                MachiningId = request.LossOrder.MachiningId
            });

            return responseData;
        }
        // Confirm Loss  Order
        public async Task<string> ConfirmLossOrderAsync(LossOrderSapRequest request)
        {
            string fetchUrl = $"{_baseUrl}ZLOSS_ORDER_CONFSet";
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
        public async Task<(object productionResult, object lossResult)> ConfirmCombinedOrderAsync(CombinedConfirmationRequest request)
        {
            // Call both SAP APIs concurrently
            var productionTask = ConfirmLossOrderAsync(request.LossOrder);
            var lossTask = ConfirmLossOrderAsync(request.LossOrder);

            await Task.WhenAll(productionTask, lossTask);

            // Deserialize both responses
            var productionResult = JsonSerializer.Deserialize<object>(productionTask.Result);
            var lossResult = JsonSerializer.Deserialize<object>(lossTask.Result);

            return (productionResult, lossResult);
        }

        public async Task<(object productionResult, object lossResult)> ConfirmProdAndLossOrderAsync(CombinedSAPConfirmationRequest request)
        {
            object productionResult = null;
            object lossResult = null;

            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                //// Step 1: Confirm Production Order in SAP
                //var productionJson = await ConfirmProductionOrderAsync(request.ProductionOrder);
                //productionResult = JsonSerializer.Deserialize<object>(productionJson);

                //bool isProductionSuccess = productionResult != null &&
                //                           !productionResult.ToString().Contains("error", StringComparison.OrdinalIgnoreCase);

                //if (!isProductionSuccess)
                //{
                //    return (productionResult, null);
                //}

                //  Step 1: Skipped for testing
                bool isProductionSuccess = true;
                productionResult = new { message = "Test mode: production step skipped" };

                // Step 2: Fetch setup + machining data from SP
                var parameters = new
                {
                    SetupId = request.LossOrder.SetupId,
                    MachiningId = request.LossOrder.MachiningId
                };

                using var multi = await connection.QueryMultipleAsync(
                    "dbo.usp_GetLossOrderByIds", parameters, transaction, commandType: CommandType.StoredProcedure);

                var setupData = (await multi.ReadAsync<SetupIdleDto>()).ToList();
                var machData = (await multi.ReadAsync<MachiningIdleDto>()).ToList();

                if (!setupData.Any() && !machData.Any())
                {
                    // Step 3: No data case
                    lossResult = new { message = "No loss data found for provided SetupId and MachiningId." };
                    return (productionResult, lossResult);
                }

                // Step 4: Prepare SAP payload
                var orderNo = setupData.FirstOrDefault()?.ORDER ?? machData.FirstOrDefault()?.ORDER;

                var sapLossItems = new List<LossOrderItem>();

                sapLossItems.AddRange(setupData.Select(s => new LossOrderItem
                {
                    ORDER = orderNo,
                    OPR_NUM = s.MSTIdleCode,
                    SETUP_IDEAL_TIME = s.SetupIdleTime.ToString(@"hh\:mm\:ss"),
                    MACH_IDEAL_TIME = "00:00:00",
                    WORKCENTER = s.WorkCenterNo
                }));

                sapLossItems.AddRange(machData.Select(m => new LossOrderItem
                {
                    ORDER = orderNo,
                    OPR_NUM = m.MSTIdleCode,
                    SETUP_IDEAL_TIME = "00:00:00",
                    MACH_IDEAL_TIME = m.MachiningIdleTime.ToString(@"hh\:mm\:ss"),
                    WORKCENTER = m.WorkCenterNo
                }));

                var lossSapRequest = new LossOrderSapRequest
                {
                    ORDER = orderNo,
                    NAV_LOSS = new LossOrderContainer
                    {
                        Results = sapLossItems
                    }
                };

                // Step 5: Call SAP Loss Order API
                var lossJson = await ConfirmLossOrderAsync(lossSapRequest);
                lossResult = JsonSerializer.Deserialize<object>(lossJson);

                // Step 6: Update IsUploadToSAP to 1 in all related tables
                var updateSql = @"
            UPDATE Trans_Setup SET IsUploadToSAP = 1 WHERE SetupId = @SetupId;
            UPDATE Trans_Setup_IdelTime SET IsUploadToSAP = 1 WHERE SetupId = @SetupId;
            UPDATE Trans_Machining SET IsUploadToSAP = 1 WHERE MachiningId = @MachiningId;
            UPDATE Trans_Machining_IdelTime SET IsUploadToSAP = 1 WHERE MachiningId = @MachiningId; " ;

                await connection.ExecuteAsync(updateSql, parameters, transaction);
                transaction.Commit(); // commit only if everything goes fine
            }
            catch (Exception ex)
            {
                transaction.Rollback(); 
               throw new Exception("Error in ConfirmProdAndLossOrderAsync", ex);
            }

            return (productionResult, lossResult);
        }



        // Brekdown Start
        public async Task<SAPBreakdownRequest?> PostBreakdownAsync(SAPBreakdownRequest request)
        {
            string url = _baseUrl + "ZBREAKDOWN_NOTIFSet";
            var (csrfToken, cookie) = await FetchCsrfTokenAsync(url);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, url);
            postRequest.Headers.Add("X-CSRF-Token", csrfToken);
            if (!string.IsNullOrEmpty(cookie))
                postRequest.Headers.Add("Cookie", cookie);

            postRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Serialize with exact casing
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });

            postRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(postRequest);

            // Log on 400 for debugging
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                await _systemLogger.LogAsync("SAP", "PostBreakdownAsync", $"SAP 400 Error: {error}");
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var envelope = JsonSerializer.Deserialize<SAPBreakdownEnvelope>(responseJson);
            return envelope?.d;
        }
        // Brekdown Stop
        public async Task<SAPBreakdownCloseRequest?> PostBreakdownCloseAsync(SAPBreakdownCloseRequest request)
        {
            string url = _baseUrl + "ZNOTIF_CLOSESet";
            var (csrfToken, cookie) = await FetchCsrfTokenAsync(url);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, url);
            postRequest.Headers.Add("X-CSRF-Token", csrfToken);
            if (!string.IsNullOrEmpty(cookie))
                postRequest.Headers.Add("Cookie", cookie);

            postRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = null });
            postRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(postRequest);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                await _systemLogger.LogAsync("SAP", "PostBreakdownCloseAsync", $"SAP Close Error: {error}");
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement.GetProperty("d");
            return JsonSerializer.Deserialize<SAPBreakdownCloseRequest>(root.GetRawText());
        }
        // Brekdown List
        public async Task<List<SAPBreakdownStatusResponse>> GetBulkBreakdownStatusesAsync(List<string> notifNums)
        {
            var validNotifNums = notifNums
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.PadLeft(12, '0'))
                .ToList();

            if (!validNotifNums.Any())
                return new List<SAPBreakdownStatusResponse>();

            string joinedNums = string.Join(",", validNotifNums);
            string url = $"{_baseUrl}ZNOTIF_INFOSet?$filter=NOTIF_NUM eq '{joinedNums}'&$format=json";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var results = doc.RootElement.GetProperty("d").GetProperty("results");

            var list = new List<SAPBreakdownStatusResponse>();
            foreach (var item in results.EnumerateArray())
            {
                list.Add(new SAPBreakdownStatusResponse
                {
                    NOTIF_NUM = item.GetProperty("NOTIF_NUM").GetString(),
                    STATUS = item.GetProperty("STATUS").GetString(),
                    NOTIF_CLOSE_DATE = item.GetProperty("NOTIF_CLOSE_DATE").GetString(),
                    NOTIF_CLOSE_TIME = item.GetProperty("NOTIF_CLOSE_TIME").GetString()
                });
            }

            return list;
        }

    }

}
