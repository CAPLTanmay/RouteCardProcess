using System.Data;
using Azure.Core;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Model;

namespace RouteCardProcess.Repositories
{
    public class SetUpTransRepository
    {
        private readonly IConfiguration _config;

        public SetUpTransRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        }

        public async Task<SetupMaster> GetByCompositeKeyAsync(string workCenterNo, string workOrderNo, string operationNo)
        {
            using var connection = CreateConnection();
            var parameters = new { WorkCenterNo = workCenterNo, WorkOrderNo = workOrderNo, OperationNo = operationNo };
            return await connection.QueryFirstOrDefaultAsync<SetupMaster>("sp_GetSetUpByCompositeKey", parameters, commandType: CommandType.StoredProcedure);
        }



        public async Task<(int Flag, string SetupStatus, string MachiningStatus, string Message, string SetUpID, string MachiningID)>
    CheckSetupNotificationStatusAsync(string workCenterNo, string workOrderNo, string operationNo)
        {
            using var connection = CreateConnection();
            var parameters = new { WorkCenterNo = workCenterNo, WorkOrderNo = workOrderNo, OperationNo = operationNo };

            var setup = await connection.QueryFirstOrDefaultAsync<SetupMaster>(
                "sp_GetSetUpByCompositeKey", parameters, commandType: CommandType.StoredProcedure);

            var machining = await connection.QueryFirstOrDefaultAsync<MachiningMaster>(
                "sp_GetMachiningByCompositeKey", parameters, commandType: CommandType.StoredProcedure);

            if (setup == null && machining == null)
                return (0, null, null, "Setup or Machining not found", null, null);

            string setupMessage = null, machiningMessage = null;

            if (setup != null)
            {
                setupMessage = setup.SetupStatus switch
                {
                    "Setup Not Start" => "Previous operator login but Not Start Setup",
                    "Setup Started" => "Previous operator Started Setup but not stopped",
                    "Setup Pause" => "Previous operator paused Setup but not stopped",
                    "Complete" => "Previous operator Completed Setup",
                    "Rework" => "Previous setup is Rework",
                    "Rejected" => "Previous setup is Rejected",
                    "Handover" => "Previous operator has handed over the setup",
                    "Setup Stopped" => "Previous operator stopped the setup but did not submit the report.",
                    _ => "Unknown setup status"
                };
            }

            if (machining != null)
            {
                machiningMessage = machining.MachiningStatus switch
                {
                    "Machining Not Started" => "Previous operator login but Machining Not Start",
                    "Machining Started" => "Previous operator Machining Started but not stopped",
                    "Machining Pause" => "Previous operator Machining paused but not stopped",
                    "Complete" => "Previous operator Machining Completed",
                    "Rework" => "Previous Machining is Rework",
                    "Rejected" => "Previous Machining is Rejected",
                    "Handover" => "Previous Machining is Handovered",
                    "Machining Stopped" => "Previous operator stopped the Machining but did not submit the report.",
                    _ => "Unknown Machining status"
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
            TimeSpan idealTime = ConvertMinutesToTimeSpan(request.IdealTime);
            var SetupId = Guid.NewGuid().ToString().Substring(0, 8);

            using var connection = CreateConnection();
            var parameters = new
            {
                request.OperatorId,
                request.WorkCenterNo,
                request.WorkOrderNo,
                request.OperationNo,
                SetUpID = SetupId,
                IdealTime = idealTime,
                SetupStatus = "Setup Not Start",
                OperatorStartTime = (DateTime?)null,
                OperatorEndTime = (DateTime?)null
            };

            try
            {
                return await connection.QuerySingleAsync<SetupMaster>("sp_CreateSetup", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547 && ex.Message.Contains("FK_SetUp_Trans_Master_LogInMaster"))
                {
                    throw new Exception("Invalid Operator ID");
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
                var existingSetup = await connection.QueryFirstOrDefaultAsync(
                    "SELECT 1 FROM SetUp_Trans_Master WHERE SetUpID = @SetUpID",
                    new { SetUpID = setUpId }
                );

                if (existingSetup == null)
                {
                    // If the setup does not exist, create a new setup
                    var setupMasterDto = new SetupMasterDto
                    {
                        SetUpID = setUpId,
                        // Add other properties like OperatorId, WorkCenterNo, etc.
                    };

                    // Call the stored procedure to create the new setup
                    await connection.ExecuteAsync("sp_CreateSetup", setupMasterDto, commandType: CommandType.StoredProcedure);

                    // After creating, proceed with starting the setup
                    await connection.ExecuteAsync("sp_StartSetup", parameters, commandType: CommandType.StoredProcedure);
                    return "Setup created and started";
                }
                else
                {
                    // If the setup exists, proceed with starting the setup
                    await connection.ExecuteAsync("sp_StartSetup", parameters, commandType: CommandType.StoredProcedure);
                    return "Setup started";
                }
            }
            catch (Exception ex)
            {

                throw new Exception($"Error starting setup: {ex.Message}", ex);
            }
        }


        public async Task<string> TogglePauseAsync(SetupPauseRequest request)
        {
            using var connection = CreateConnection();
            try
            {
                var setupInfo = await connection.QueryFirstOrDefaultAsync<dynamic>("sp_GetSetupStatusAndOperator", new { SetUpID = request.SetUpID }, commandType: CommandType.StoredProcedure);

                if (setupInfo == null) return "Invalid Setup ID";

                string status = setupInfo.SetupStatus;
                string operatorId = setupInfo.OperatorId;

                if (status == "Setup Started")
                {
                    var parameters = new { SetUpID = request.SetUpID, OperatorId = operatorId, PauseCode = request.PauseCode };
                    await connection.ExecuteAsync("sp_TogglePause_Start", parameters, commandType: CommandType.StoredProcedure);
                    return "Setup paused";
                }
                else if (status == "Setup Pause")
                {
                    await connection.ExecuteAsync("sp_TogglePause_Resume", new { SetUpID = request.SetUpID }, commandType: CommandType.StoredProcedure);
                    return "Setup resumed";
                }

                return "Invalid setup status";
            }
            catch (Exception ex)
            {
                throw new Exception("Error toggling pause", ex);
            }
        }

        public async Task<bool> EndSetupTimeAsync(string setUpId)
        {
            using var connection = CreateConnection();
            var parameters = new { SetUpID = setUpId };
            var setupInfo = await connection.QueryFirstOrDefaultAsync<dynamic>("sp_GetSetupStatusAndOperator", new { SetUpID = setUpId }, commandType: CommandType.StoredProcedure);
            string status = setupInfo.SetupStatus;
            if (status == "Setup Pause")
            {
                await connection.ExecuteAsync("sp_TogglePause_Resume", new { SetUpID = setUpId }, commandType: CommandType.StoredProcedure);
            }
            var rowsAffected = await connection.ExecuteAsync("sp_EndSetupTime", parameters, commandType: CommandType.StoredProcedure);
            return rowsAffected > 0;
        }

        public async Task<bool> InsertDelaysAsync(SetupDelayRequest request)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var setup = await connection.QueryFirstOrDefaultAsync<dynamic>("sp_GetSetupOperatorAndStatus", new { SetUpID = request.SetUpID }, transaction, commandType: CommandType.StoredProcedure);

                if (setup == null) return false;

                TimeSpan totalDelay = TimeSpan.Zero;
                foreach (var delay in request.Delays)
                {
                    totalDelay += delay.DelayTime;
                }

                foreach (var delay in request.Delays)
                {
                    await connection.ExecuteAsync("sp_InsertDelays", new
                    {
                        SetUpID = request.SetUpID,
                        OperatorId = setup.OperatorId,
                        SetupStatus = request.SetUpStatus,
                        DelayReasonCode = delay.DelayReasonCode,
                        DelayTime = delay.DelayTime,
                        TotalDelayedTime = totalDelay
                    }, transaction, commandType: CommandType.StoredProcedure);
                }

                await connection.ExecuteAsync("sp_UpdateSetupStatus", new { request.SetUpStatus, SetUpID = request.SetUpID }, transaction, commandType: CommandType.StoredProcedure);
                if (request.SetUpStatus == "Complete")
                {
                    await connection.ExecuteAsync("sp_UpdateSetupEndTime", new { EndTime = DateTime.Now, SetUpID = request.SetUpID }, transaction, commandType: CommandType.StoredProcedure);
                }
                else
                {
                    await connection.ExecuteAsync("sp_UpdateSetupEndTime", new { EndTime = DateTime.Now, SetUpID = request.SetUpID }, transaction, commandType: CommandType.StoredProcedure);
                }

                transaction.Commit();
                return true;
            }
            catch
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
                throw new ArgumentException("Invalid minutes format");
            }
        }
    }
}
