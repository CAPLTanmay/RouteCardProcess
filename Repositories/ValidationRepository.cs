using System.Data;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.RouteCardReport;
using RouteCardProcess.Model.DTOs.SapValidation;

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
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Throw custom exception with SAP error JSON as message
                throw new HttpRequestException(content, null, response.StatusCode);
            }

            return content;
        }

        public async Task<string> ValidateOrderAsync(string order, string workCenter)
        {
            string url = $"{_baseUrl}ZVALIDATE_ORDERSet(ORDER='{order}',WORK_CENTER='{workCenter}')?$format=json";

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Throw custom exception with response body
                throw new HttpRequestException(content, null, response.StatusCode);
            }

            return content;
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
        //public async Task<string> UpdateWorkCenterAsync(WorkCenterUpdateRequest request)
        //{
        //    string fetchUrl = $"{_baseUrl}ZWC_UPDATESet";
        //    var (csrfToken, cookie) = await FetchCsrfTokenAsync(fetchUrl);

        //    var postRequest = new HttpRequestMessage(HttpMethod.Post, fetchUrl);
        //    postRequest.Headers.Add("X-CSRF-Token", csrfToken);

        //    if (!string.IsNullOrEmpty(cookie))
        //        postRequest.Headers.Add("Cookie", cookie);

        //    postRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //    var json = JsonSerializer.Serialize(request);
        //    postRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        //    var response = await _httpClient.SendAsync(postRequest);
        //    response.EnsureSuccessStatusCode();

        //    return await response.Content.ReadAsStringAsync();
        //}


        public async Task<bool> UpdateWorkCenterAsync(WorkCenterUpdateRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@OrderNumber", request.ORDER_NUMBER);
            parameters.Add("@OperationNo", request.OPERATION);
            parameters.Add("@OldWorkCenter", request.OLD_WORKCENTER);
            parameters.Add("@NewWorkCenter", request.WORK_CENTER);
            parameters.Add("@WorkCenterText", request.WORK_CENTER_TEXT);

            var result = await connection.ExecuteAsync(
                "usp_UpdateWorkCenterSAP",
                parameters,
                commandType: CommandType.StoredProcedure);

            return result > 0;
        }

        // Confirm Production Order
        public async Task<string> ConfirmProductionOrderAsync(CombinedSAPConfirmationRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var persNo = request.ProductionOrder?.NAV_CONF?.FirstOrDefault()?.PERS_NO;

            if (request.ProductionOrder?.NAV_CONF != null)
            {
                foreach (var nav in request.ProductionOrder.NAV_CONF)
                {
                    if (!string.IsNullOrEmpty(nav.PERS_NO))
                    {
                        // check employee
                        var employee = await GetEmployeeByContractEmpIdAsync(nav.PERS_NO);
                        nav.PERS_DUMMY_NO = "";

                        if (employee != null && employee.IsContractEmployee)
                        {
                            // replace PERS_NO with EmployeeCode
                            nav.PERS_DUMMY_NO = nav.PERS_NO;
                            nav.PERS_NO = employee.EmployeeCode;
                        }
                    }
                }
            }

            // Step 1: Confirm Production Order in SAP
            string fetchUrl = $"{_baseUrl}ZCONFIRMSet";
            var (csrfToken, cookie) = await FetchCsrfTokenAsync(fetchUrl);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, fetchUrl);
            postRequest.Headers.Add("X-CSRF-Token", csrfToken);

            if (!string.IsNullOrEmpty(cookie))
                postRequest.Headers.Add("Cookie", cookie);

            postRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var json = JsonSerializer.Serialize(request.ProductionOrder);
            postRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(postRequest);
            var responseData = await response.Content.ReadAsStringAsync();

            //  Custom handling instead of blindly EnsureSuccessStatusCode
            if (!response.IsSuccessStatusCode)
            {
                // Throw HttpRequestException but include SAP response JSON
                throw new HttpRequestException(responseData, null, response.StatusCode);
            }

            // Step 2: Update DB flags using SP (only if SAP call succeeded)
            var operatorId1 = request.ProductionOrder?.NAV_CONF?.FirstOrDefault()?.PERS_NO;

            await connection.ExecuteAsync(
                "usp_UpdateUploadToSAP_ProductionOrder",
                new
                {
                    SetupId = request.LossOrder.SetupId,
                    MachiningId = request.LossOrder.MachiningId,
                    OperatorId = persNo,
                },
                commandType: CommandType.StoredProcedure
            );

            return responseData;
        }

        // Confirm Loss Order
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
            var responseData = await response.Content.ReadAsStringAsync();

            // Custom handling instead of blindly EnsureSuccessStatusCode
            if (!response.IsSuccessStatusCode)
            {
                // Throw HttpRequestException with SAP response JSON so controller can parse it
                throw new HttpRequestException(responseData, null, response.StatusCode);
            }

            return responseData;
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
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            string sapError = null;
            object productionResult = null;
            object lossResult = null;

            try
            {
                // Step 1: SAP call for Production Order (NO DB update here)
                var productionJson = await ConfirmProductionOrderAsync(request);
                productionResult = JsonSerializer.Deserialize<object>(productionJson);

                bool isProductionSuccess = productionResult != null &&
                                           !productionResult.ToString().Contains("error", StringComparison.OrdinalIgnoreCase);

                if (!isProductionSuccess)
                {
                    sapError = ValidationRepository.ExtractSapErrorMessage(productionJson);
                    transaction.Rollback();
                    //return (productionResult, null);
                    return (new { success = false, type = "Production", data = sapError }, null);
                }

                // Step 2: Prepare Loss Order payload from SP
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
                    //lossResult = new { message = "No loss data found for provided SetupId and MachiningId." };
                    ////transaction.Rollback();
                    //return (productionResult, lossResult);

                    transaction.Commit();

                    lossResult = new
                    {
                        message = "No loss data found for provided SetupId and MachiningId."
                    };

                    return (productionResult, lossResult);
                }

                // Step 3: Build SAP payload for Loss
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
                    NAV_LOSS = new LossOrderContainer { Results = sapLossItems }
                };

                // Step 4: SAP Loss Order API call
                var lossJson = await ConfirmLossOrderAsync(lossSapRequest);
                lossResult = JsonSerializer.Deserialize<object>(lossJson);

                bool isLossSuccess = lossResult != null && !lossResult.ToString().Contains("error", StringComparison.OrdinalIgnoreCase);

                if (!isLossSuccess)
                {
                    transaction.Rollback();
                    return (productionResult, lossResult);
                }

                // Step 5: Now update DB for both only after SAP success
                var operatorId1 = request.ProductionOrder?.NAV_CONF?.FirstOrDefault()?.PERS_NO;

                await connection.ExecuteAsync(
                    "usp_UpdateUploadToSAP_ProductionOrder",
                    new { SetupId = request.LossOrder.SetupId, MachiningId = request.LossOrder.MachiningId, OperatorId = operatorId1 },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure
                );

                await connection.ExecuteAsync(
                    "usp_UpdateUploadToSAP_LossOrder",
                    new { SetupId = request.LossOrder.SetupId, MachiningId = request.LossOrder.MachiningId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure
                );

                transaction.Commit();
            }
            //catch (Exception ex)
            //{

            //    transaction.Rollback();
            //    string sapMessage = ValidationRepository.ExtractSapErrorMessage(ex.Message);
            //    //throw new Exception("Error in ConfirmProdAndLossOrderAsync", ex);
            //    return (new { success = false, type = "System", message = sapMessage }, null);
            //}

            catch (Exception)
            {
                transaction.Rollback();
                throw;
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

        public static string ExtractSapErrorMessage(string sapErrorJson)
        {
            if (string.IsNullOrWhiteSpace(sapErrorJson))
                return "Unknown SAP error occurred";

            try
            {
                using var doc = JsonDocument.Parse(sapErrorJson);

                // Root -> error
                if (doc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    // error.innererror.errordetails[0].message
                    if (errorElement.TryGetProperty("innererror", out var innerErrorElement) &&
                        innerErrorElement.TryGetProperty("errordetails", out var errorDetailsElement))
                    {
                        foreach (var detail in errorDetailsElement.EnumerateArray())
                        {
                            if (detail.TryGetProperty("message", out var messageElement))
                            {
                                string? message = messageElement.GetString();
                                if (!string.IsNullOrWhiteSpace(message))
                                    return message.Trim();
                            }
                        }
                    }

                    // Fallback: error.message.value
                    if (errorElement.TryGetProperty("message", out var msgElement))
                    {
                        if (msgElement.TryGetProperty("value", out var valueElement))
                        {
                            string? message = valueElement.GetString();
                            if (!string.IsNullOrWhiteSpace(message))
                                return message.Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Optional: Log exception if needed
                return $"Failed to parse SAP error: {ex.Message}";
            }

            return "Unknown SAP error occurred";
        }
        public async Task<ConEmployee?> GetEmployeeByContractEmpIdAsync(string contractEmpId)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            return await connection.QueryFirstOrDefaultAsync<ConEmployee>("dbo.usp_GetEmployeeByContractEmpId", new { ContractEmpId = contractEmpId },commandType: CommandType.StoredProcedure );
        }
    }
}
