using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Model;

public class MachiningRepository
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public MachiningRepository(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("DefaultConnection");
    }

    private IDbConnection Connection => new SqlConnection(_connectionString);

    public async Task<MachiningMaster> CreateAsync(MachiningDto obj)
    {
        using var connection = Connection;
        var MachiningId = Guid.NewGuid().ToString().Substring(0, 8);
        var result = await connection.QueryFirstOrDefaultAsync<MachiningMaster>(
            "sp_CreateMachining",
            new
            {
                obj.OperatorId,
                obj.WorkCenterNo,
                obj.WorkOrderNo,
                obj.OperationNo,
                MachiningID=MachiningId,
                IdealTime = TimeSpan.TryParse(obj.IdealTime, out var idealTime) ? idealTime : TimeSpan.Zero,
                obj.TotalQty,
                obj.ProcessedQty
            },
            commandType: CommandType.StoredProcedure
        );
        return result;
    }

  

    public async Task<string> StartMachiningAsync(string machiningId)
    {
        using var connection = Connection; // Assuming same factory method
        var parameters = new { MachiningID = machiningId };

        try
        {
            // Check if the Machining ID exists in the database
            var existingMachining = await connection.QueryFirstOrDefaultAsync(
                "SELECT 1 FROM Machining_Trans_Master WHERE MachiningID = @MachiningID",
                parameters
            );

            if (existingMachining == null)
            {
                // If not found, create a new machining entry
                var machiningMasterDto = new MachiningDto
                {
                    MachiningId = machiningId,
                    // Populate other necessary properties: OperatorID, WorkOrderNo, etc.
                };

                await connection.ExecuteAsync("sp_CreateMachining", machiningMasterDto, commandType: CommandType.StoredProcedure);

                // Start machining after creating it
                await connection.ExecuteAsync("sp_StartMachining", parameters, commandType: CommandType.StoredProcedure);
                return "Machining created and started";
            }
            else
            {
                // Start machining if it already exists
                await connection.ExecuteAsync("sp_StartMachining", parameters, commandType: CommandType.StoredProcedure);
                return "Machining started";
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error starting machining: {ex.Message}", ex);
        }
    }


    public async Task TogglePauseAsync(string machiningId, string pauseCode)
    {
        using var connection = Connection;
        await connection.ExecuteAsync(
            "sp_ToggleMachiningPause",
            new { MachiningID = machiningId, PauseCode = pauseCode },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<bool> EndMachiningAsync(string machiningId)
    {
        using var connection = Connection; // Use the same connection factory
        var parameters = new { MachiningID = machiningId };

        // 1. Get current status
        var machiningInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "sp_GetMachiningStatusAndOperator",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        string status = machiningInfo?.MachiningStatus;

        // 2. If paused, toggle resume
        if (status == "Machining Pause")
        {
            await connection.ExecuteAsync("sp_ToggleMachiningPause_Resume", parameters, commandType: CommandType.StoredProcedure);
        }

        // 3. End machining
        var rowsAffected = await connection.ExecuteAsync("sp_EndMachining", parameters, commandType: CommandType.StoredProcedure);

        // 4. Update end time
        if (rowsAffected > 0)
        {
            await connection.ExecuteAsync("sp_UpdateMachiningEndTime",
                new { EndTime = DateTime.Now, MachiningID = machiningId },
                commandType: CommandType.StoredProcedure);
            return true;
        }

        return false;
    }


    public async Task AddQuantitiesAsync(string machiningId, int totalQty, int processedQty, string qtyStatus)
    {
        using var connection = Connection;
        await connection.ExecuteAsync(
            "sp_AddQuantities",
            new
            {
                MachiningID = machiningId,
                TotalQty = totalQty,
                ProcessedQty = processedQty,
                QtyStatus = qtyStatus
            },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task AddDelaysAsync(string machiningId, int processedQty, TimeSpan delayTime, string reasonCode, TimeSpan totalDelayedTime)
    {
        using var connection = Connection;
        await connection.ExecuteAsync(
            "sp_AddMachiningDelays",
            new
            {
                MachiningID = machiningId,
                ProcessedQty = processedQty,
                ProcessQtyDelayTime = delayTime,
                ReasonCode = reasonCode,
                TotalDelayedTime = totalDelayedTime
            },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<MachiningMaster> GetByCompositeKeyAsync(string workCenterNo, string workOrderNo, string operationNo)
    {
        using var connection = Connection;
        var result = await connection.QueryFirstOrDefaultAsync<MachiningMaster>(
            "dbo.sp_GetMachiningByCompositeKey",
            new { WorkCenterNo = workCenterNo, WorkOrderNo = workOrderNo, OperationNo = operationNo },
            commandType: CommandType.StoredProcedure
        );
        return result;
    }
}
