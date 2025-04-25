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

        public async Task<MachiningMaster> GetByCompositeKeyAsync(string workCenterNo, string workOrderNo, string operationNo)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var sql = @"SELECT TOP 1 * FROM Machining_Trans_Master
                        WHERE WorkCenterNo = @workCenterNo AND WorkOrderNo = @workOrderNo AND OperationNo = @operationNo";
            return await connection.QueryFirstOrDefaultAsync<MachiningMaster>(sql, new { workCenterNo, workOrderNo, operationNo });
        }

        public async Task<MachiningMaster> CreateMachiningAsync(MachiningDto request)
        {
            TimeSpan idealTime = ConvertMinutesToTimeSpan(request.IdealTime);
            var machiningId = Guid.NewGuid().ToString().Substring(0, 8);

            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            try
            {
                // Step 1: Check if OperatorId exists
                var checkSql = "SELECT COUNT(1) FROM LogInMaster WHERE OperatorId = @OperatorId";
                int exists = await connection.ExecuteScalarAsync<int>(checkSql, new { request.OperatorId });

                if (exists == 0)
                {
                    throw new Exception($"Invalid Operator ID: '{request.OperatorId}' does not exist.");
                }

                // Step 2: Insert
                var sql = @"INSERT INTO Machining_Trans_Master
            (OperatorId, WorkCenterNo, WorkOrderNo, OperationNo, MachiningID, IdealTime, MachiningStatus, OperatorStartTime, OperatorEndTime, TotalQty, ProcessedQty)
            OUTPUT INSERTED.*
            VALUES
            (@OperatorId, @WorkCenterNo, @WorkOrderNo, @OperationNo, @MachiningID, @IdealTime, @MachiningStatus, @OperatorStartTime, @OperatorEndTime, @TotalQty, @ProcessedQty)";

                var parameters = new
                {
                    request.OperatorId,
                    request.WorkCenterNo,
                    request.WorkOrderNo,
                    request.OperationNo,
                    request.TotalQty,
                    request.ProcessedQty,
                    MachiningID = machiningId,
                    IdealTime = idealTime,
                    MachiningStatus = "Machining Not Started",
                    OperatorStartTime = (DateTime?)null,
                    OperatorEndTime = (DateTime?)null,
                };

                var result = await connection.QuerySingleOrDefaultAsync<MachiningMaster>(sql, parameters);
                if (result == null)
                {
                    throw new Exception("Failed to insert machining record.");
                }

                return result;
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547 && ex.Message.Contains("FK_Machining_Master_LogInMaster"))
                {
                    throw new Exception("Foreign Key violation: Invalid Operator ID.");
                }

                throw;
            }
        }



        // Helper method to convert minutes to TimeSpan
        private TimeSpan ConvertMinutesToTimeSpan(string minutes)
        {
            // Try to parse the string to double
            if (double.TryParse(minutes.Trim(), out double parsedMinutes))
            {
                int totalMinutes = (int)parsedMinutes;
                int hours = totalMinutes / 60;
                int mins = totalMinutes % 60;
                return new TimeSpan(hours, mins, 0);
            }
            else
            {
                // You can handle invalid format however you'd like. Throwing an exception for now.
                throw new ArgumentException("Invalid minutes format");
            }
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

                if (machining.MachiningStatus != "Machining Not Started")
                    return "Machining already started or invalid status";

                var now = DateTime.Now;

                await connection.ExecuteAsync(
                    @"UPDATE Machining_Trans_Master 
                      SET MachiningStatus = 'Machining Started', OperatorStartTime = @Now 
                      WHERE MachiningID = @MachiningID",
                    new { Now = now, MachiningID = machiningId }, transaction);

                await connection.ExecuteAsync(
                    @"INSERT INTO Machining_Details_Master (MachiningID, MachiningStartTime,TotalQty,ProcessedQty)
                      VALUES (@MachiningID, @MachiningStartTime,@TotalQty,@ProcessedQty)",
                    new
                    {
                        MachiningID = machiningId,
                        MachiningStartTime = now,
                        TotalQty = machining.TotalQty,
                        ProcessedQty = machining.ProcessedQty ?? 0
                    }, transaction);

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
                    new { MachiningID = request.MachiningId }, transaction);

                if (machining == null)
                    return "Machining ID not found";

                var now = DateTime.Now;

                if (machining.MachiningStatus == "Machining Started")
                {
                    await connection.ExecuteAsync(
                        @"INSERT INTO Machining_Pause_Master (MachiningID, OperatorId, PauseStartTime, PauseCode)
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
                        @"SELECT TOP 1 * FROM Machining_Pause_Master
                          WHERE MachiningID = @MachiningID AND PauseEndTime IS NULL
                          ORDER BY SrNo DESC",
                        new { machining.MachiningID }, transaction);

                    if (pause == null)
                        return "No active pause record found";

                    await connection.ExecuteAsync(
                        @"UPDATE Machining_Pause_Master
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

        public async Task<bool> AddQuantitiesAsync(AddQuantity request)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            string getOperatorSql = @"
            SELECT OperatorId 
            FROM Machining_Trans_Master 
            WHERE MachiningId = @MachiningId";

            var operatorId = await connection.ExecuteScalarAsync<string>(getOperatorSql, new { request.MachiningId });

            string sql = @"
            INSERT INTO Qty_Bifurcation_Details 
            (MachiningId, OperatorId, TotalQty, ProcessedQty, ProcessedQtyTime, QtyStatus)
            VALUES 
            (@MachiningId, @OperatorId, @TotalQty, @ProcessedQty, GETDATE(), @QtyStatus)";

            
            foreach (var item in request.QuantityList)
            {
                var parameters = new
                {
                    MachiningId = request.MachiningId,
                    OperatorId = operatorId,
                    TotalQty = request.TotalQty,
                    ProcessedQty = item.ProcessedQty,
                    QtyStatus = item.MachiningStatus
                };

                await connection.ExecuteAsync(sql, parameters);
            }

            return true;
        }

        public async Task<bool> AddMachiningDelaysAsync(MachiningDelayRequest request)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            // Step 1: Get OperatorId from Machining_Trans_Master
            string getOperatorSql = @"
            SELECT OperatorId 
            FROM Machining_Trans_Master 
            WHERE MachiningId = @MachiningId";

            var operatorId = await connection.ExecuteScalarAsync<string>(getOperatorSql, new { request.MachiningId });

            if (string.IsNullOrEmpty(operatorId))
                throw new Exception("OperatorId not found for the given MachiningId");

            // Step 2: Insert into Machining_Delay_Master
            string insertSql = @"
            INSERT INTO Machining_Delay_Master 
            (MachiningId, OperatorId, MachiningStatus, TotalDelayedTime, ProcessQty, ProcessQtyDelayTime, ReasonCode)
            VALUES 
            (@MachiningId, @OperatorId, @MachiningStatus, @TotalDelayedTime, @ProcessedQty, @ProcessQtyDelayTime, @ReasonCode)";

            foreach (var delay in request.Delays)
            {
                var parameters = new
                {
                    MachiningId = request.MachiningId,
                    OperatorId = operatorId,
                    MachiningStatus = "Delayed", // Adjust this based on your logic
                    TotalDelayedTime = request.TotalDelayedTime,
                    ProcessedQty = delay.ProcessedQty,
                    ProcessQtyDelayTime = delay.DelayTime,
                    ReasonCode = delay.DelayReasonCode
                };

                await connection.ExecuteAsync(insertSql, parameters);
            }

            return true;
        }


    }
}
