using System.Collections.Generic;
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

        public async Task<SetupMaster> GetByCompositeKeyAsync(string workCenterNo, string workOrderNo, string operationNo)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var sql = @"SELECT TOP 1 * FROM SetUp_Trans_Master
                        WHERE WorkCenterNo = @workCenterNo AND WorkOrderNo = @workOrderNo AND OperationNo = @operationNo";
            return await connection.QueryFirstOrDefaultAsync<SetupMaster>(sql, new { workCenterNo, workOrderNo, operationNo });
        }

        public async Task<SetupMaster> CreateSetupAsync(SetupMasterDto request)
        {
            TimeSpan idealTime = ConvertMinutesToTimeSpan(request.IdealTime);
            var setupId = Guid.NewGuid().ToString().Substring(0, 8);

            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            var sql = @"INSERT INTO SetUp_Trans_Master
                (OperatorId, WorkCenterNo, WorkOrderNo, OperationNo, SetUpID, IdealTime, SetupStatus, OperatorStartTime, OperatorEndTime)
                OUTPUT INSERTED.*
                VALUES
                (@OperatorId, @WorkCenterNo, @WorkOrderNo, @OperationNo, @SetUpID, @IdealTime, @SetupStatus, @OperatorStartTime, @OperatorEndTime)";

            var parameters = new
            {
                request.OperatorId,
                request.WorkCenterNo,
                request.WorkOrderNo,
                request.OperationNo,
                SetUpID = setupId,
                IdealTime = idealTime,
                SetupStatus = "Setup Not Start",
                OperatorStartTime = (DateTime?)null,
                OperatorEndTime = (DateTime?)null,
            };

            try
            {
                return await connection.QuerySingleAsync<SetupMaster>(sql, parameters);
            }
            catch (SqlException ex)
            {
                // Check for Foreign Key violation (SQL error code 547)
                if (ex.Number == 547 && ex.Message.Contains("FK_SetUp_Trans_Master_LogInMaster"))
                {
                    throw new Exception("Invalid Operator ID");
                }

                // Re-throw other errors
                throw;
            }
        }

        // Helper method to convert minutes to TimeSpan
        private TimeSpan ConvertMinutesToTimeSpan(double minutes)
        {
            int totalMinutes = (int)minutes;
            int hours = totalMinutes / 60;
            int mins = totalMinutes % 60;
            return new TimeSpan(hours, mins, 0);
        }

        public async Task<string> StartSetupAsync(string setUpId)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var setup = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT * FROM SetUp_Trans_Master WHERE SetUpID = @SetUpID",
                    new { SetUpID = setUpId }, transaction);

                if (setup == null)
                    return "Setup not found";

                /* if (setup.SetupStatus != "Setup Not Start")
                     return "Setup already started or invalid status";
                comment just for testing*/

                var now = DateTime.Now;

                // Update master setup status and start time
                await connection.ExecuteAsync(
                    @"UPDATE SetUp_Trans_Master 
              SET SetupStatus = 'Setup Started', OperatorStartTime = @Now 
              WHERE SetUpID = @SetUpID",
                    new { Now = now, SetUpID = setUpId }, transaction);

                // Insert new entry in details master
                await connection.ExecuteAsync(
                    @"INSERT INTO SetUp_Trans_Details_Master (SetUpID, SetupStartTime)
              VALUES (@SetUpID, @SetupStartTime)",
                    new { SetUpID = setUpId, SetupStartTime = now }, transaction);

                transaction.Commit();
                return "Setup started";
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public async Task<string> TogglePauseAsync(SetupPauseRequest request)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Get setup by SetUpID
                var setup = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT * FROM SetUp_Trans_Master WHERE SetUpID = @SetUpID",
                    new { SetUpID = request.SetUpID }, transaction);

                if (setup == null)
                    return "Setup ID not found";

                var now = DateTime.Now;

                if (setup.SetupStatus == "Setup Started")
                {
                    // Start Pause
                    await connection.ExecuteAsync(
                        @"INSERT INTO SetUp_Trans_Pause_Master (SetUpID, OperatorId, PauseStartTime,PauseCode)
                  VALUES (@SetUpID, @OperatorId, @PauseStartTime,@PauseCode)",
                        new { SetUpID = setup.SetUpID, OperatorId = setup.OperatorId, PauseStartTime = now, PauseCode = request.PauseCode }, transaction);

                    await connection.ExecuteAsync(
                        @"UPDATE SetUp_Trans_Master
                  SET SetupStatus = 'Setup Pause'
                  WHERE SetUpID = @SetUpID",
                        new { setup.SetUpID }, transaction);

                    transaction.Commit();
                    return "Setup paused";
                }
                else if (setup.SetupStatus == "Setup Pause")
                {
                    // Resume
                    var pause = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        @"SELECT TOP 1 * FROM SetUp_Trans_Pause_Master
                  WHERE SetUpID = @SetUpID AND PauseEndTime IS NULL
                  ORDER BY SrNo DESC",
                        new { setup.SetUpID }, transaction);

                    if (pause == null)
                        return "No active pause record found";

                    await connection.ExecuteAsync(
                        @"UPDATE SetUp_Trans_Pause_Master
                  SET PauseEndTime = @PauseEndTime
                  WHERE SrNo = @SrNo",
                        new { PauseEndTime = now, SrNo = pause.SrNo }, transaction);

                    await connection.ExecuteAsync(
                        @"UPDATE SetUp_Trans_Master
                  SET SetupStatus = 'Setup Started'
                  WHERE SetUpID = @SetUpID",
                        new { setup.SetUpID }, transaction);

                    transaction.Commit();
                    return "Setup resumed";
                }
                return "Invalid setup status";
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> EndSetupTimeAsync(string setUpId)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            var sql = @"UPDATE SetUp_Trans_Master
                SET OperatorEndTime = GETDATE(),
                    SetupStatus = 'Setup Stopped'
                WHERE SetUpID = @SetUpID";

            var rowsAffected = await connection.ExecuteAsync(sql, new { SetUpID = setUpId });

            return rowsAffected > 0;
        }


        public async Task<bool> InsertDelaysAsync(SetupDelayRequest request)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Fetch operator and status from master
                var setup = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT OperatorId, SetupStatus FROM SetUp_Trans_Master WHERE SetUpID = @SetUpID",
                    new { request.SetUpID }, transaction);

                if (setup == null) return false;

                // Step 1: Calculate total delay time
                TimeSpan totalDelay = TimeSpan.Zero;
                foreach (var delay in request.Delays)
                {
                    totalDelay += delay.DelayTime;
                }


                // Step 2: Insert each delay record
                foreach (var delay in request.Delays)
                {
                    await connection.ExecuteAsync(@"
                INSERT INTO Setup_Delay_Master 
                (SetUpID, OperatorId, SetupStatus, DelayReasonCode, DelayTime, TotalDelayedTime)
                VALUES 
                (@SetUpID, @OperatorId, @SetupStatus, @DelayReasonCode, @DelayTime, @TotalDelayedTime)",
                        new
                        {
                            SetUpID = request.SetUpID,
                            OperatorId = setup.OperatorId,
                            SetupStatus = request.SetUpStatus,
                            delay.DelayReasonCode,
                            delay.DelayTime,
                            TotalDelayedTime = totalDelay.ToString(), // or totalDelay.TotalMinutes, etc.
                        }, transaction);
                }

                // Step 3: Update SetupStatus in SetUp_Trans_Master
                await connection.ExecuteAsync(@"
            UPDATE SetUp_Trans_Master
            SET SetupStatus = @SetupStatus
            WHERE SetUpID = @SetUpID",
                    new { request.SetUpStatus, SetUpID = request.SetUpID }, transaction);

                // Step 4: If SetupStatus is 'Complete', update SetupEndTime
                if (request.SetUpStatus == "Complete")
                {
                    await connection.ExecuteAsync(@"
                UPDATE SetUp_Trans_Details_Master
                SET SetupEndTime = @EndTime
                WHERE SetUpID = @SetUpID",
                        new
                        {
                            EndTime = DateTime.Now,
                            SetUpID = request.SetUpID
                        }, transaction);
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

    }
}
