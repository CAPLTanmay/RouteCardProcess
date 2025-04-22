using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Model;

namespace RouteCardProcess.Repositories
{
    public class MachiningRepository
    {
        private readonly IConfiguration _config;

        public MachiningRepository(IConfiguration config)
        {
            _config = config;
        }

        public async Task<MachiningMaster> GetByCompositeKeyAsync(string workCentreNo, string workOrderNo, string operationNo)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var sql = @"SELECT TOP 1 * FROM Machining_Trans_Master
                        WHERE WorkCentreNo = @workCentreNo AND WorkOrderNo = @workOrderNo AND OperationNo = @operationNo";
            return await connection.QueryFirstOrDefaultAsync<MachiningMaster>(sql, new { workCentreNo, workOrderNo, operationNo });
        }

        public async Task<MachiningMaster> CreateMachiningAsync(MachiningMaster request)
        {
            var machiningId = Guid.NewGuid().ToString().Substring(0, 8);
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            var sql = @"INSERT INTO Machining_Trans_Master
                        (OperatorId, WorkCentreNo, WorkOrderNo, OperationNo, MachiningID, IdealTime, MachiningStatus, OperatorStartTime, OperatorEndTime)
                        OUTPUT INSERTED.*
                        VALUES
                        (@OperatorId, @WorkCentreNo, @WorkOrderNo, @OperationNo, @MachiningID, @IdealTime, @MachiningStatus, @OperatorStartTime, @OperatorEndTime)";

            var parameters = new
            {
                request.OperatorId,
                request.WorkCenterNo,
                request.WorkOrderNo,
                request.OperationNo,
                MachiningID = machiningId,
                request.IdealTime,
                MachiningStatus = "Machining Not Start",
                OperatorStartTime = (DateTime?)null,
                OperatorEndTime = (DateTime?)null,
            };

            return await connection.QuerySingleAsync<MachiningMaster>(sql, parameters);
        }

        public async Task<string> StartMachiningAsync(string machiningId)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var machining = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT * FROM Machining_Trans_Master WHERE MachiningID = @MachiningID",
                    new { MachiningID = machiningId }, transaction);

                if (machining == null)
                    return "Machining not found";

                if (machining.MachiningStatus != "Machining Not Start")
                    return "Machining already started or invalid status";

                var now = DateTime.Now;

                await connection.ExecuteAsync(
                    @"UPDATE Machining_Trans_Master 
                      SET MachiningStatus = 'Machining Started', OperatorStartTime = @Now 
                      WHERE MachiningID = @MachiningID",
                    new { Now = now, MachiningID = machiningId }, transaction);

                await connection.ExecuteAsync(
                    @"INSERT INTO Machining_Trans_Details_Master (MachiningID, MachiningStartTime)
                      VALUES (@MachiningID, @MachiningStartTime)",
                    new { MachiningID = machiningId, MachiningStartTime = now }, transaction);

                transaction.Commit();
                return "Machining started";
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<string> TogglePauseAsync(MachiningPauseRequest request)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var machining = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT * FROM Machining_Trans_Master WHERE MachiningID = @MachiningID",
                    new { MachiningID = request.MachiningID }, transaction);

                if (machining == null)
                    return "Machining ID not found";

                var now = DateTime.Now;

                if (machining.MachiningStatus == "Machining Started")
                {
                    await connection.ExecuteAsync(
                        @"INSERT INTO Machining_Trans_Pause_Master (MachiningID, OperatorId, PauseStartTime, PauseCode)
                          VALUES (@MachiningID, @OperatorId, @PauseStartTime, @PauseCode)",
                        new { MachiningID = machining.MachiningID, OperatorId = machining.OperatorId, PauseStartTime = now, PauseCode = request.PauseCode }, transaction);

                    await connection.ExecuteAsync(
                        @"UPDATE Machining_Trans_Master
                          SET MachiningStatus = 'Machining Pause'
                          WHERE MachiningID = @MachiningID",
                        new { machining.MachiningID }, transaction);

                    transaction.Commit();
                    return "Machining paused";
                }
                else if (machining.MachiningStatus == "Machining Pause")
                {
                    var pause = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        @"SELECT TOP 1 * FROM Machining_Trans_Pause_Master
                          WHERE MachiningID = @MachiningID AND PauseEndTime IS NULL
                          ORDER BY SrNo DESC",
                        new { machining.MachiningID }, transaction);

                    if (pause == null)
                        return "No active pause record found";

                    await connection.ExecuteAsync(
                        @"UPDATE Machining_Trans_Pause_Master
                          SET PauseEndTime = @PauseEndTime
                          WHERE SrNo = @SrNo",
                        new { PauseEndTime = now, SrNo = pause.SrNo }, transaction);

                    await connection.ExecuteAsync(
                        @"UPDATE Machining_Trans_Master
                          SET MachiningStatus = 'Machining Started'
                          WHERE MachiningID = @MachiningID",
                        new { machining.MachiningID }, transaction);

                    transaction.Commit();
                    return "Machining resumed";
                }

                return "Invalid machining status";
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> EndMachiningTimeAsync(string machiningId)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));

            var sql = @"UPDATE Machining_Trans_Master
                        SET OperatorEndTime = GETDATE(),
                            MachiningStatus = 'Machining Stopped'
                        WHERE MachiningID = @MachiningID";

            var rowsAffected = await connection.ExecuteAsync(sql, new { MachiningID = machiningId });

            return rowsAffected > 0;
        }
        public async Task<bool> InsertDelaysAsync(MachiningDelayRequest request)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var machining = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT OperatorId, MachiningStatus FROM Machining_Trans_Master WHERE MachiningID = @MachiningID",
                    new { request.MachiningID }, transaction);

                if (machining == null) return false;

                TimeSpan totalDelay = TimeSpan.Zero;
                foreach (var delay in request.Delays)
                {
                    totalDelay += delay.DelayTime;
                }

                foreach (var delay in request.Delays)
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO Machining_Delay_Master 
                        (MachiningID, OperatorId, MachiningStatus, DelayReasonCode, DelayTime, TotalDelayedTime)
                        VALUES 
                        (@MachiningID, @OperatorId, @MachiningStatus, @DelayReasonCode, @DelayTime, @TotalDelayedTime)",
                        new
                        {
                            MachiningID = request.MachiningID,
                            OperatorId = machining.OperatorId,
                            MachiningStatus = request.MachiningStatus,
                            delay.DelayReasonCode,
                            delay.DelayTime,
                            TotalDelayedTime = totalDelay.ToString()
                        }, transaction);
                }

                if (request.MachiningStatus == "Complete")
                {
                    await connection.ExecuteAsync(@"
                        UPDATE Machining_Trans_Details_Master
                        SET MachiningEndTime = @EndTime
                        WHERE MachiningID = @MachiningID",
                        new
                        {
                            EndTime = DateTime.Now,
                            MachiningID = request.MachiningID
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
