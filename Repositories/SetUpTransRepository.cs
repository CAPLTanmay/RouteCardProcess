using System.Data;
using System.Text.Json.Serialization;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Setup;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class SetUpTransRepository : ISetUpTransRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IUserMessageService _userMessageService;
        private readonly IBreakDownRepository _repo;
        public SetUpTransRepository(SqlConnectionFactory connectionFactory, IUserMessageService userMessageService, IBreakDownRepository repo)
        {
            _connectionFactory = connectionFactory;
            _userMessageService = userMessageService;
            _repo = repo;
        }
        private IDbConnection CreateConnection() => _connectionFactory.CreateConnection();
        public async Task<SetupMaster> GetByCompositeKeyAsync(SetupCompositeKeyRequest request)
        {
            using var connection = CreateConnection();
            var parameters = new { WorkCenterNo = request.WorkCenterNo, WorkOrderNo = request.WorkOrderNo, OperationNo = request.OperationNo };
            return await connection.QueryFirstOrDefaultAsync<SetupMaster>("usp_GetSetUpByCompositeKey", parameters, commandType: CommandType.StoredProcedure);
        }
        public async Task<(int Flag, string SetupStatus, string MachiningStatus, string Message, string SetUpID, string MachiningID, bool Breakdown)>
        CheckSetupNotificationStatusAsync(string workCenterNo, string workOrderNo, string operationNo)
        {
            using var connection = CreateConnection();
            var parameters = new { WorkCenterNo = workCenterNo, WorkOrderNo = workOrderNo, OperationNo = operationNo };

            var setup = await connection.QueryFirstOrDefaultAsync<SetupMaster>(
                "usp_GetSetUpByCompositeKey", parameters, commandType: CommandType.StoredProcedure);

            var MachiningParameters = new
            {
                WorkCenterNo = workCenterNo,
                ProductionOrderNo = workOrderNo,
                OperationNo = operationNo
            };

            var machining = await connection.QueryFirstOrDefaultAsync<MachiningMaster>(
                "usp_GetMachiningByCompositeKey", MachiningParameters, commandType: CommandType.StoredProcedure);

            // Step 0: Sync breakdown status from SAP
            var data = await _repo.GetAllBreakDownsAsync();


            // New breakdown flag logic
            var breakdownRow = await connection.QueryFirstOrDefaultAsync<string>("usp_GetLatestBreakdownStatusByWorkCenter",
                new
                {
                    WorkCenterNo = workCenterNo
                }, commandType: CommandType.StoredProcedure);

            bool isBreakdownActive = false;
            if (breakdownRow != null)
            {
                if (breakdownRow == "" || !breakdownRow.Equals("Notification completed", StringComparison.OrdinalIgnoreCase))
                {
                    isBreakdownActive = true;
                }
            }

            if (setup == null && machining == null)
            {
                var sapRoutingData = await connection.QueryFirstOrDefaultAsync<dynamic>("usp_GetConfirmedVsTotalQty",
                new
                {
                    ProductionOrderNo = workOrderNo,
                    WorkCenterNo = workCenterNo,
                    OperationNo = operationNo
                }, commandType: CommandType.StoredProcedure);

                if (sapRoutingData != null)
                {
                    // Convert to int with null fallback to avoid nullable comparison issues
                    int confirmedQty = sapRoutingData.S_ConfirmedQuantity ?? 0;
                    int totalQty = sapRoutingData.TotalQty ?? 0;

                    if (confirmedQty == totalQty && totalQty != 0)
                    {
                        var SAPMachiningStatus = "Completed"; // or _userMessageService.GetMessage(1084)
                        var SAPMessage = _userMessageService.GetMessage(1053); // Machining completed

                        return (
                            Flag: 1,
                            SetupStatus: "",
                            MachiningStatus: SAPMachiningStatus,
                            Message: SAPMessage,
                            SetUpID: "",
                            MachiningID: "",
                             Breakdown: isBreakdownActive
                        );
                    }
                }
                var message1036 = _userMessageService.GetMessage(1036) ?? "Message not configured";

                return (
                    Flag: 0,
                    SetupStatus: "",
                    MachiningStatus: "",
                    Message: message1036,
                    SetUpID: "",
                    MachiningID: "",
                     Breakdown: isBreakdownActive
                );
            }

            string setupMessage = string.Empty, machiningMessage = string.Empty;

            if (setup != null)
            {
                setupMessage = setup.SetupStatus switch
                {
                    var s when s == _userMessageService.Messages[1073] => _userMessageService.GetMessage(1037),
                    var s when s == _userMessageService.Messages[1074] => _userMessageService.GetMessage(1038),
                    var s when s == _userMessageService.Messages[1075] => _userMessageService.GetMessage(1039),
                    var s when s == _userMessageService.Messages[1076] => _userMessageService.GetMessage(1040),
                    var s when s == _userMessageService.Messages[1077] => _userMessageService.GetMessage(1041),
                    var s when s == _userMessageService.Messages[1078] => _userMessageService.GetMessage(1042),
                    var s when s == _userMessageService.Messages[1079] => _userMessageService.GetMessage(1043),
                    var s when s == _userMessageService.Messages[1080] => _userMessageService.GetMessage(1044),
                    _ => _userMessageService.GetMessage(1045)
                };
            }

            if (machining != null)
            {
                machiningMessage = machining.MachiningStatus switch
                {
                    var s when s == _userMessageService.GetMessage(1081) => _userMessageService.GetMessage(1046),
                    var s when s == _userMessageService.GetMessage(1082) => _userMessageService.GetMessage(1047),
                    var s when s == _userMessageService.GetMessage(1083) => _userMessageService.GetMessage(1048),
                    var s when s == _userMessageService.GetMessage(1076) => _userMessageService.GetMessage(1049),
                    var s when s == _userMessageService.GetMessage(1077) => _userMessageService.GetMessage(1050),
                    var s when s == _userMessageService.GetMessage(1078) => _userMessageService.GetMessage(1051),
                    var s when s == _userMessageService.GetMessage(1079) => _userMessageService.GetMessage(1052),
                    var s when s == _userMessageService.GetMessage(1084) => _userMessageService.GetMessage(1053),
                    _ => _userMessageService.GetMessage(1054)
                };
            }

            string combinedMessage = string.Join(" | ", new[] { setupMessage, machiningMessage }.Where(msg => !string.IsNullOrWhiteSpace(msg)));
            return (
                 Flag: 1,
                 SetupStatus: setup?.SetupStatus ?? "",
                 MachiningStatus: machining?.MachiningStatus ?? "",
                 Message: combinedMessage,
                 SetUpID: setup?.SetUpID ?? "",
                 MachiningID: machining?.MachiningId ?? "",
                Breakdown: isBreakdownActive
            );
        }
        public async Task<SetupMaster> CreateSetupAsync(SetupMasterDto request)
        {
            // TimeSpan StandardSetupTime = ConvertMinutesToTimeSpan(request.StandardSetupTime);
            var SetupId = Guid.NewGuid().ToString().Substring(0, 8);

            TimeSpan standardSetupTime;
            if (TimeSpan.TryParse(request.StandardSetupTime, out var tsValue))
            {
                standardSetupTime = tsValue;
            }
            else if (double.TryParse(request.StandardSetupTime, out var mins))
            {
                standardSetupTime = TimeSpan.FromMinutes(mins);
            }
            else
            {
                standardSetupTime = TimeSpan.Zero;
            }

            using var connection = CreateConnection();

            var parameters = new
            {
                request.OperatorId,
                request.WorkCenterNo,
                request.DepartmentId,
                request.OperationNo,
                request.ProductionOrderNo,
                SetUpID = SetupId,
                StandardSetupTime = standardSetupTime,
                //request.StandardSetupTime,
                SetupStatus = _userMessageService.GetMessage(1073),
                OperatorStartTime = (DateTime?)null,
                OperatorEndTime = (DateTime?)null
            };

            try
            {
                return await connection.QuerySingleAsync<SetupMaster>("usp_CreateSetup", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547 && ex.Message.Contains("FK_SetUp_Trans_Master_LogInMaster"))
                {
                    throw new Exception(_userMessageService.GetMessage(1061));
                }
                throw;
            }
        }
        public async Task<SetupStartResponse> StartSetupAsync(SetupIdentifierRequest request)
        {
            using var connection = CreateConnection();

            try
            {
                var existingSetup = await connection.QueryFirstOrDefaultAsync<int?>(
                    "usp_CheckSetupExists",
                    new { SetUpID = request.SetUpID },
                    commandType: CommandType.StoredProcedure
                );

                if (existingSetup == null)
                {
                    var setupMasterDto = new SetupMasterDto
                    {
                        SetUpID = request.SetUpID,
                        // Add other properties like OperatorId, WorkCenterNo, etc.
                    };

                    await connection.ExecuteAsync(
                        "usp_CreateSetup",
                        setupMasterDto,
                        commandType: CommandType.StoredProcedure
                    );
                }

                //  Use DynamicParameters for OUTPUT
                var parameters = new DynamicParameters();
                parameters.Add("@SetUpID", request.SetUpID);
                parameters.Add("@OperatorStartTime", dbType: DbType.DateTime, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "usp_StartSetup",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var operatorStartTime = parameters.Get<DateTime>("@OperatorStartTime");

                var message = existingSetup == null
                    ? _userMessageService.GetMessage(1055)
                    : _userMessageService.GetMessage(1056);

                return new SetupStartResponse
                {
                    Message = message,
                    StartDate = operatorStartTime.ToString("yyyy-MM-dd"),
                    StartTime = operatorStartTime.ToString("HH:mm:ss")
                };
            }
            catch (Exception ex)
            {
                var errorPrefix = _userMessageService.GetMessage(1089);
                throw new Exception($"{errorPrefix} {ex.Message}", ex);
            }
        }

        public async Task<string> TogglePauseAsync(SetupPauseRequest request)
        {
            using var connection = CreateConnection();
            try
            {
                var setupInfo = await connection.QueryFirstOrDefaultAsync<dynamic>("usp_GetSetupStatusAndOperator", new { SetUpID = request.SetUpID }, commandType: CommandType.StoredProcedure);

                if (setupInfo == null) return _userMessageService.GetMessage(1057);

                string status = setupInfo.SetupStatus;
                string operatorId = setupInfo.OperatorId;

                if (status == _userMessageService.GetMessage(1074))
                {
                    var parameters = new { SetUpID = request.SetUpID, OperatorId = operatorId, PauseCode = request.PauseCode };
                    await connection.ExecuteAsync("usp_TogglePause_Start", parameters, commandType: CommandType.StoredProcedure);
                    return _userMessageService.GetMessage(1075);
                }
                else if (status == _userMessageService.GetMessage(1075))
                {
                    var parameters = new
                    {
                        SetUpID = request.SetUpID,
                        ResumeReasonCode = request.PauseCode
                    };

                    await connection.ExecuteAsync("usp_TogglePause_Resume", parameters, commandType: CommandType.StoredProcedure);

                    return _userMessageService.GetMessage(1059);
                }

                return _userMessageService.GetMessage(1060);
            }
            catch (Exception ex)
            {
                throw new Exception(_userMessageService.GetMessage(1088), ex);
            }
        }

        public async Task<EndSetupResultDto> EndSetupTimeAsync(SetupIdentifierRequest request)
        {
            using var connection = CreateConnection();
            var parameters = new { SetUpID = request.SetUpID };

            var setupInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "usp_GetSetupStatusAndOperator",
                new { SetUpID = request.SetUpID },
                commandType: CommandType.StoredProcedure);

            string status = setupInfo?.SetupStatus;
            string standardSetupTime = setupInfo?.StandardSetupTime != null
    ? TimeSpan.Parse(setupInfo.StandardSetupTime.ToString()).ToString(@"hh\:mm\:ss")
    : null;


            if (status == _userMessageService.GetMessage(1075))
            {
                await connection.ExecuteAsync("usp_TogglePause_Resume", new { SetUpID = request.SetUpID }, commandType: CommandType.StoredProcedure);
            }

            var rowsAffected = await connection.ExecuteAsync("usp_EndSetupTime", parameters, commandType: CommandType.StoredProcedure);

            if (rowsAffected > 0)
            {
                var setupData = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT SetupStartTime, SetupEndTime, TotalSetupTime 
              FROM Trans_Setup 
              WHERE SetupId = @SetUpID",
                    new { SetUpID = request.SetUpID });

                string timeDiff = null;
                string setupStartTimeStr = null;
                string setupEndTimeStr = null;

                if (setupData != null)
                {
                    timeDiff = ((TimeSpan)setupData.TotalSetupTime).ToString(@"hh\:mm\:ss");
                    // Format start/end times to HH:mm:ss
                    setupStartTimeStr = ((DateTime)setupData.SetupStartTime).ToString("HH:mm:ss");
                    setupEndTimeStr = ((DateTime)setupData.SetupEndTime).ToString("HH:mm:ss");
                }

                return new EndSetupResultDto
                {
                    Success = true,
                    Message = _userMessageService.GetMessage(1027),
                    TimeDiff = timeDiff,
                    SetupStartTime = setupStartTimeStr,
                    SetupEndTime = setupEndTimeStr,
                    StandardSetupTime = standardSetupTime
                };
            }

            return new EndSetupResultDto
            {
                Success = false,
                Message = _userMessageService.GetMessage(1087),
                TimeDiff = null
            };
        }

        public async Task<bool> InsertDelaysAsync(SetupDelayRequest request)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Get operator info once
                var setup = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_GetSetupStatusAndOperator",
                    new { SetUpID = request.SetUpID },
                    transaction,
                    commandType: CommandType.StoredProcedure
                );

                if (setup == null)
                {
                    transaction.Rollback();
                    return false;
                }
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
                                OperatorId = setup.OperatorId,
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
                                OperatorId = setup.OperatorId,
                                idle.MSTIdleCode,
                                idle.SetupIdleTime
                            },
                            transaction,
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }

                // Update setup status
                await connection.ExecuteAsync(
                    "usp_UpdateSetupStatus",
                    new { request.SetUpStatus, SetUpID = request.SetUpID },
                    transaction,
                    commandType: CommandType.StoredProcedure
                );

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
        }
        private TimeSpan ConvertMinutesToTimeSpan(string minutes)
        {
            if (double.TryParse(minutes.Trim(), out double parsedMinutes))
            {
                int totalMinutes = (int)parsedMinutes;
                int hours = totalMinutes / 60;
                int mins = totalMinutes % 60;
                return new TimeSpan(hours, mins, 0);
            }
            else
            {
                throw new ArgumentException(_userMessageService.GetMessage(1090));
            }
        }
        public async Task InsertSetupOperatorStartAsync(string setupId, string operatorId, DateTime startTime)
        {
            using var connection = _connectionFactory.CreateConnection();

            var parameters = new
            {
                SetupId = setupId,
                OperatorId = operatorId,
                StartTime = startTime
            };

            await connection.ExecuteAsync(
                "usp_InsertSetupOperatorStart",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public class JsonDateTimeConverter : JsonConverter<DateTime?>
        {
            public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return DateTime.TryParse(reader.GetString(), out var date) ? date : (DateTime?)null;
            }

            public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                    writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                else
                    writer.WriteNullValue();
            }
        }
    }
}
