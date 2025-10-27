using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Manualdata;
using RouteCardProcess.Model.DTOs.ManualData;
using RouteCardProcess.Model.DTOs.RouteCardReport;
using RouteCardProcess.Model.DTOs.SapSync;
using RouteCardProcess.Model.DTOs.SapValidation;

namespace RouteCardProcess.Repositories
{
    public class MaualDataRepository : IManualDataRepository

    {
        private readonly HttpClient _httpClient;
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly string _baseUrl;
        private readonly string _materialBaseUrl;

        public MaualDataRepository(HttpClient httpClient, IConfiguration configuration, SqlConnectionFactory connectionFactory)
        {
            _httpClient = httpClient;
            _connectionFactory = connectionFactory;

            var username = configuration["SapSettings:Username"];
            var password = configuration["SapSettings:Password"];
            _baseUrl = configuration["SapSettings:BaseUrl"];
            _materialBaseUrl = configuration["RoutingData:MaterialBaseUrl"];

            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
        private TimeSpan ParseToTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return TimeSpan.Zero;
            value = value.Trim().Split('.')[0];
            return int.TryParse(value, out var minutes) ? TimeSpan.FromMinutes(minutes) : TimeSpan.Zero;
        }
        private int ParseToInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = value.Trim().Split('.')[0];
            return int.TryParse(value, out var result) ? result : 0;
        }
        public async Task SyncManualDataAsync(MaualDataRequest request)
        {
            var url = $"{_baseUrl}ZROUTING_DATASet/?$filter=ORDER_NUMBER eq '{request.ProductionOrderNumber}'&$format=json";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var sapResponse = JsonSerializer.Deserialize<SapRoutingResponse>(json);

            if (sapResponse?.d?.results == null || !sapResponse.d.results.Any())
                return;

            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var item in sapResponse.d.results)
                {
                    int totalQty = ParseToInt(item.TARGET_QUANTITY);
                    int confirmedQty = ParseToInt(item.CONFIRMED_QUANTIT);
                    TimeSpan stdSetupTime = ParseToTime(item.SETUP_TIME);
                    TimeSpan stdMachiningTime = ParseToTime(item.PROCESSING_TIME);

                    var existingData = await connection.QueryFirstOrDefaultAsync<int?>(
                       "usp_GetCompletedQty",
                        new { WorkOrder = item.ORDER_NUMBER, OperationNo = item.OPERATION_NUMBER },
                        transaction: transaction);

                    int completedQty = confirmedQty;
                    if (existingData.HasValue && existingData.Value >= confirmedQty)
                        completedQty = existingData.Value;

                    var parameters = new
                    {
                        WorkOrder = item.ORDER_NUMBER,
                        WorkCenter = item.WORK_CENTER,
                        OperationNo = item.OPERATION_NUMBER,
                        TotalQty = totalQty,
                        S_ConfirmedQuantity = confirmedQty,
                        L_CompletedQty = completedQty,
                        OperationDescription = item.DESCRIPTION,
                        WorkCenterText = item.WORK_CENTER_TEXT,
                        StdSetupTime = stdSetupTime,
                        StdMachiningTime = stdMachiningTime,
                        S_RoutingDataStatus = item.STATUS ?? "",
                        SetupUnit = item.SETUP_UNIT ?? "MIN",
                        ProcessingUnit = item.PROCESSING_UNIT ?? "MIN",
                        Material = item.MATERIAL,
                        MaterialText = item.MATERIAL_TEXT,
                        OrderType = item.ORDER_TYPE,
                        ProductionPlant = item.PRODUCTION_PLANT,
                        ProductionUnit = item.UNIT,
                        MrpController = item.MRP_CONTROLLER,
                        ProductionScheduler = item.PRODUCTION_SCHEDULER,
                        ControlKey = item.CONTROL_KEY
                    };

                    await connection.ExecuteAsync(
                "usp_SyncManualData",
                parameters,
                transaction: transaction,
                commandType: CommandType.StoredProcedure);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<ManualDataResponseDto>> GetManualDataAsync(GetMaualDataRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();

            var data = (await connection.QueryAsync<ManualDataResponseDto>(
                     "usp_GetManualData",
                     new { OrderNumber = request.ProductionOrderNumber, WorkCenter = request.WorkCenter, },
                     commandType: CommandType.StoredProcedure)).ToList();

            foreach (var item in data)
            {
                item.MaterialTextLink = $"{_materialBaseUrl}{Regex.Replace(item.Material, @"\d{3}$", "")}";
            }
            return data;
        }
        public async Task<ManualDataUpdateResult> UpdateManualDataAsync(ManualDataUpdateDto dto)
        {
            using var connection = _connectionFactory.CreateConnection();

            // 1️⃣ Validate Operator via stored procedure
            var employee = await connection.QueryFirstOrDefaultAsync(
                "usp_GetEmployeeLoginInfo",
                new { dto.OperatorId },
                commandType: CommandType.StoredProcedure);

            if (employee == null)
            {
                return new ManualDataUpdateResult
                {
                    Success = false,
                    OperatorId = dto.OperatorId,
                    Message = "Invalid or inactive user."
                };
            }

            var parameters = new DynamicParameters();
            parameters.Add("@WorkOrder", dto.WorkOrder);
            parameters.Add("@WorkCenter", dto.WorkCenter);
            parameters.Add("@OperationNo", dto.OperationNo);
            parameters.Add("@OperatorId", dto.OperatorId);
            parameters.Add("@L_CompletedQty", dto.L_CompletedQty);
            parameters.Add("@SetupStartTime", dto.SetupStartTime);
            parameters.Add("@SetupEndTime", dto.SetupEndTime);
            parameters.Add("@MachiningStartTime", dto.MachiningStartTime);
            parameters.Add("@MachiningEndTime", dto.MachiningEndTime);
            parameters.Add("@SetupId", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
            parameters.Add("@MachiningId", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "usp_UpdateManualData",
                parameters,
                commandType: CommandType.StoredProcedure);

            var setupId = parameters.Get<string>("@SetupId");
            var machiningId = parameters.Get<string>("@MachiningId");

            var dtoResult = new ManualDataUpdateResult
            {
                Success = setupId != null || machiningId != null,
                SetupId = setupId,
                MachiningId = machiningId,
                OperatorId = dto.OperatorId,
                Message = setupId != null || machiningId != null
        ? "Manual data updated successfully."
        : "No matching record found or update failed."
            };

            return dtoResult;
        }

        public async Task<bool> InsertDelaysAsync(ManualSetupDelayRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {

                // Insert Exceptions if any
                if (request.Exceptions?.Any() == true)
                {
                    foreach (var exception in request.Exceptions)
                    {
                        await connection.ExecuteAsync(
                            "usp_InsertSetupException",
                            new
                            {
                                SetUpID = request.SetUpID,
                                OperatorId = request.OperatorId,
                                exception.ExceptionsReasonCode,
                                exception.Std_exceptions_ReasonCode,
                                exception.ExceptionsTime,
                                exception.Std_exceptions_Remark
                            },
                            transaction,
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }

                // Insert Idle Times if any
                if (request.IdleTimes?.Any() == true)
                {
                    foreach (var idle in request.IdleTimes)
                    {
                        await connection.ExecuteAsync(
                            "usp_InsertSetupIdle",
                            new
                            {
                                SetUpID = request.SetUpID,
                                OperatorId = request.OperatorId,
                                idle.MSTIdleCode,
                                idle.SetupIdleTime
                            },
                            transaction,
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
        }
        public async Task<bool> AddDelaysAsync(ManualMachiningDelayRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {


                // Insert Exceptions if any
                if (request.Exceptions?.Any() == true)
                {
                    foreach (var exception in request.Exceptions)
                    {
                        await connection.ExecuteAsync(
                            "usp_InsertMachiningException",
                            new
                            {
                                MachiningID = request.MachiningId,
                                OperatorId = request.OperatorId,
                                exception.ExceptionsReasonCode,
                                exception.Std_exceptions_ReasonCode,
                                exception.ExceptionsTime,
                                exception.Std_exceptions_Remark
                            },
                            transaction,
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }

                // Insert Idle Times if any
                if (request.IdleTimes?.Any() == true)
                {
                    foreach (var idle in request.IdleTimes)
                    {
                        await connection.ExecuteAsync(
                            "usp_InsertMachiningIdle",
                            new
                            {
                                MachiningID = request.MachiningId,
                                OperatorId = request.OperatorId,
                                idle.MSTIdleCode,
                                idle.MachiningIdleTime
                            },
                            transaction,
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        public async Task<IEnumerable<RouteCardReportDto>> GetManualReportAsync(RouteCardReportFilterRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            //  Pad ProductionOrderNo before using it in the query
            var paddedOrderNo = request.ProductionOrderNo?.PadLeft(12, '0');
            var operatorId = request.ReqOperatorId;

            var result = await connection.QueryAsync<RouteCardReportDto>(
                "usp_GetManualReport",
                new
                {
                    OperatorId = operatorId,
                    request.ConfirmationDate,
                    ProductionOrderNo = paddedOrderNo,
                    request.WorkCenterNo,
                    Dept = request.Department
                },
                commandType: CommandType.StoredProcedure);

            return result;
        }
        public async Task<IEnumerable<RouteCardReportDto>> GetUploadedManualReportAsync(RouteCardReportFilterRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            //  Pad ProductionOrderNo before using it in the query
            var paddedOrderNo = request.ProductionOrderNo?.PadLeft(12, '0');
            var operatorId = request.ReqOperatorId;

            var result = await connection.QueryAsync<RouteCardReportDto>(
                "usp_GetUploadedManualReport",
                new
                {
                    OperatorId = operatorId,
                    request.ConfirmationDate,
                    ProductionOrderNo = paddedOrderNo,
                    request.WorkCenterNo,
                    Dept = request.Department
                },
                commandType: CommandType.StoredProcedure);

            return result;
        }
        public async Task<TimingInfoDto?> GetManualTimingInfo(OrderReportRequestDto request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var timingInfo = new TimingInfoDto();

            if (!string.IsNullOrEmpty(request.MachiningId) ||
                    (!request.MachiningOperatorTransactionId.HasValue || request.MachiningOperatorTransactionId == Guid.Empty))
            {
                var machiningResult = await connection.QueryFirstOrDefaultAsync<TimingInfoDto>(
                    "usp_GetManualMachiningInfo",
                    new
                    {
                        MachiningId = request.MachiningId,
                        MachiningOperatorTransactionId = request.MachiningOperatorTransactionId
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (machiningResult != null)
                {
                    timingInfo.StandardMachiningTime = machiningResult.StandardMachiningTime;
                    timingInfo.MachiningStartDate = machiningResult.MachiningStartDate;
                    timingInfo.MachiningStartTime = machiningResult.MachiningStartTime;
                    timingInfo.MachiningEndDate = machiningResult.MachiningEndDate;
                    timingInfo.MachiningEndTime = machiningResult.MachiningEndTime;
                    timingInfo.TotalMachiningTime = machiningResult.TotalMachiningTime;
                    timingInfo.CompletedQty = machiningResult.CompletedQty;
                    timingInfo.MachiningId = request.MachiningId;

                    timingInfo.MachiningOperatorId = machiningResult.MachiningOperatorId;
                    timingInfo.MachiningOperatorStartDate = machiningResult.MachiningOperatorStartDate;
                    timingInfo.MachiningOperatorStartTime = machiningResult.MachiningOperatorStartTime;
                    timingInfo.MachiningOperatorEndDate = machiningResult.MachiningOperatorEndDate;
                    timingInfo.MachiningOperatorEndTime = machiningResult.MachiningOperatorEndTime;
                    timingInfo.MachiningTotalOperatorTime = machiningResult.MachiningTotalOperatorTime;

                    timingInfo.MachiningOperatorTransactionId = request.MachiningOperatorTransactionId;
                }
            }

            if (!string.IsNullOrEmpty(request.SetupId) ||
               (!request.OperatorTransactionId.HasValue || request.OperatorTransactionId == Guid.Empty))
            {
                var setupResult = await connection.QueryFirstOrDefaultAsync<TimingInfoDto>(
                    "usp_GetManualSetupInfo",
                    new
                    {
                        SetupId = request.SetupId,
                        OperatorTransactionId = request.OperatorTransactionId
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (setupResult != null)
                {
                    timingInfo.StandardSetupTime = setupResult.StandardSetupTime;
                    timingInfo.SetupStartDate = setupResult.SetupStartDate;
                    timingInfo.SetupStartTime = setupResult.SetupStartTime;
                    timingInfo.SetupEndDate = setupResult.SetupEndDate;
                    timingInfo.SetupEndTime = setupResult.SetupEndTime;
                    timingInfo.TotalSetupTime = setupResult.TotalSetupTime;
                    timingInfo.SetupId = request.SetupId;

                    timingInfo.OperatorTransactionId = setupResult.OperatorTransactionId;
                    timingInfo.SetupOperatorId = setupResult.SetupOperatorId;
                    timingInfo.SetupOperatorStartDate = setupResult.SetupOperatorStartDate;
                    timingInfo.SetupOperatorStartTime = setupResult.SetupOperatorStartTime;
                    timingInfo.SetupOperatorEndDate = setupResult.SetupOperatorEndDate;
                    timingInfo.SetupOperatorEndTime = setupResult.SetupOperatorEndTime;
                    timingInfo.SetupTotalOperatorTime = setupResult.SetupTotalOperatorTime;
                }
            }

            return timingInfo;
        }
        // Confirm Production Order
        public async Task<string> ConfirmManualOrderAsync(CombinedSAPConfirmationRequest request)
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
                        var employee = await GetEmpContractEmpIdAsync(nav.PERS_NO);

                        if (employee != null && employee.IsContractEmployee)
                        {
                            // replace PERS_NO with EmployeeCode
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
                "usp_UpdateUploadToSAP_ManualData",
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

        public async Task<ConEmployee?> GetEmpContractEmpIdAsync(string contractEmpId)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            return await connection.QueryFirstOrDefaultAsync<ConEmployee>("dbo.usp_GetEmployeeByContractEmpId", new { ContractEmpId = contractEmpId }, commandType: CommandType.StoredProcedure);
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
    }
}
