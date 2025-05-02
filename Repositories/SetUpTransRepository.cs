using System.Data;
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
                await connection.ExecuteAsync("sp_StartSetup", parameters, commandType: CommandType.StoredProcedure);
                return "Setup started";
            }
            catch (Exception ex)
            {
                throw new Exception("Error starting setup", ex);
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
