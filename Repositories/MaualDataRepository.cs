using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Machining;
using RouteCardProcess.Model.DTOs.Manualdata;
using RouteCardProcess.Model.DTOs.ManualData;
using RouteCardProcess.Model.DTOs.SapSync;
using RouteCardProcess.Model.DTOs.Setup;

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
                OperatorId = dto.OperatorId
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
    }
}
