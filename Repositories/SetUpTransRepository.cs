using System.Data;
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
        public SetUpTransRepository(SqlConnectionFactory connectionFactory, IUserMessageService userMessageService)
        {
            _connectionFactory = connectionFactory;
            _userMessageService = userMessageService;
        }

        private IDbConnection CreateConnection() => _connectionFactory.CreateConnection();

        public async Task<SetupMaster> GetByCompositeKeyAsync(string workCenterNo, string workOrderNo, string operationNo)
        {
            using var connection = CreateConnection();
            var parameters = new { WorkCenterNo = workCenterNo, WorkOrderNo = workOrderNo, OperationNo = operationNo };
            return await connection.QueryFirstOrDefaultAsync<SetupMaster>("usp_GetSetUpByCompositeKey", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<(int Flag, string SetupStatus, string MachiningStatus, string Message, string SetUpID, string MachiningID)>
        CheckSetupNotificationStatusAsync(string workCenterNo, string workOrderNo, string operationNo)
        {
            using var connection = CreateConnection();
            var parameters = new { WorkCenterNo = workCenterNo, WorkOrderNo = workOrderNo, OperationNo = operationNo };

            var setup = await connection.QueryFirstOrDefaultAsync<SetupMaster>(
                "usp_GetSetUpByCompositeKey", parameters, commandType: CommandType.StoredProcedure);

            var machining = await connection.QueryFirstOrDefaultAsync<MachiningMaster>(
                "usp_GetMachiningByCompositeKey", parameters, commandType: CommandType.StoredProcedure);

            if (setup == null && machining == null)
                return (0, null, null, _userMessageService.GetMessage(1036), null, null);

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
                SetupStatus: setup?.SetupStatus,
                MachiningStatus: machining?.MachiningStatus,
                Message: combinedMessage,
                SetUpID: setup?.SetUpID,
                MachiningID: machining?.MachiningId
            );
        }

        public async Task<SetupMaster> CreateSetupAsync(SetupMasterDto request)
        {
           // TimeSpan StandardSetupTime = ConvertMinutesToTimeSpan(request.StandardSetupTime);
            var SetupId = Guid.NewGuid().ToString().Substring(0, 8);

            using var connection = CreateConnection();
            var parameters = new
            {
                request.OperatorId,
                request.WorkCenterNo,
                request.OperationNo,
                request.ProductionOrderNo,
                SetUpID = SetupId,
                //StandardSetupTime = StandardSetupTime,
                request.StandardSetupTime,
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

        public async Task<string> StartSetupAsync(string setUpId)
        {
            using var connection = CreateConnection();
            var parameters = new { SetUpID = setUpId };

            try
            {
                // Check if the Setup ID exists in the database
                var existingSetup = await connection.QueryFirstOrDefaultAsync<int?>(
             "usp_CheckSetupExists",
             parameters,
             commandType: CommandType.StoredProcedure);


                if (existingSetup == null)
                {
                    // If the setup does not exist, create a new setup
                    var setupMasterDto = new SetupMasterDto
                    {
                        SetUpID = setUpId,
                        // Add other properties like OperatorId, WorkCenterNo, etc.
                    };

                    // Call the stored procedure to create the new setup
                    await connection.ExecuteAsync("usp_CreateSetup", setupMasterDto, commandType: CommandType.StoredProcedure);

                    // After creating, proceed with starting the setup
                    await connection.ExecuteAsync("usp_StartSetup", parameters, commandType: CommandType.StoredProcedure);
                    return _userMessageService.GetMessage(1055);
                }
                else
                {
                    // If the setup exists, proceed with starting the setup
                    await connection.ExecuteAsync("usp_StartSetup", parameters, commandType: CommandType.StoredProcedure);
                    return _userMessageService.GetMessage(1056);
                }
            }
            catch (Exception ex)
            {
                var errorPrefix = _userMessageService.GetMessage(1089); // "Error starting setup:"
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

        public async Task<bool> EndSetupTimeAsync(string setUpId)
        {
            using var connection = CreateConnection();
            var parameters = new { SetUpID = setUpId };

            var setupInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "usp_GetSetupStatusAndOperator",
                new { SetUpID = setUpId },
                commandType: CommandType.StoredProcedure);

            string status = setupInfo?.SetupStatus;

            if (status == _userMessageService.GetMessage(1075))
            {
                await connection.ExecuteAsync("usp_TogglePause_Resume", new { SetUpID = setUpId }, commandType: CommandType.StoredProcedure);
            }

            var rowsAffected = await connection.ExecuteAsync("usp_EndSetupTime", parameters, commandType: CommandType.StoredProcedure);

            if (rowsAffected > 0)
            {
                await connection.ExecuteAsync("usp_UpdateSetupEndTime",
                    new { EndTime = DateTime.Now, SetUpID = setUpId },
                    commandType: CommandType.StoredProcedure);
                return true;
            }

            return false;
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

                // Insert Delays if any
                if (request.Delays?.Any() == true)
                {
                    TimeSpan totalDelay = request.Delays.Aggregate(TimeSpan.Zero, (sum, d) => sum + d.DelayTime);

                    foreach (var delay in request.Delays)
                    {
                        await connection.ExecuteAsync(
                            "usp_InsertDelays",
                            new
                            {
                                SetUpID = request.SetUpID,
                                OperatorId = setup.OperatorId,
                                SetupStatus = request.SetUpStatus,
                                DelayReasonCode = delay.DelayReasonCode,
                                DelayTime = delay.DelayTime,
                                TotalDelayedTime = totalDelay
                            },
                            transaction,
                            commandType: CommandType.StoredProcedure
                        );
                    }
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
                                SetupStatus = request.SetUpStatus,
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
                                SetupStatus = request.SetUpStatus,
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

    }
}
