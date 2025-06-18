using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Machining;
using RouteCardProcess.Model.Entities;
using RouteCardProcess.Repositories;

public class MachiningRepository : IMachiningRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly IUserMessageService _userMessageService;

    public MachiningRepository(SqlConnectionFactory connectionFactory, IUserMessageService userMessageService)
    {
        _connectionFactory = connectionFactory;
        _userMessageService = userMessageService;
    }

    private IDbConnection CreateConnection() => _connectionFactory.CreateConnection();

    public async Task<MachiningMaster> CreateAsync(MachiningDto obj)
    {
        using var connection = CreateConnection();
        var MachiningId = Guid.NewGuid().ToString().Substring(0, 8);
        var result = await connection.QueryFirstOrDefaultAsync<MachiningMaster>(
            "usp_CreateMachining",
            new
            {
                obj.OperatorId,
                obj.WorkCenterNo,
                obj.WorkOrderNo,
                obj.OperationNo,
                MachiningID = MachiningId,
                IdealTime = double.TryParse(obj.IdealTime, out var minutes)
    ? TimeSpan.FromMinutes(minutes)
    : TimeSpan.Zero,

                obj.TotalQty,
                obj.ProcessedQty
            },
            commandType: CommandType.StoredProcedure
        );
        return result;
    }

    public async Task<string> StartMachiningAsync(string machiningId)
    {
        using var connection = CreateConnection(); // Assuming same factory method
        var parameters = new { MachiningID = machiningId };

        try
        {
            // Check if the Machining ID exists in the database
            var existingMachining = await connection.QueryFirstOrDefaultAsync<int?>(
           "usp_CheckMachiningExists",
           parameters,
           commandType: CommandType.StoredProcedure);

            if (existingMachining == null)
            {
                // If not found, create a new machining entry
                var machiningMasterDto = new MachiningDto
                {
                    MachiningId = machiningId,
                    // Populate other necessary properties: OperatorID, WorkOrderNo, etc.
                };

                await connection.ExecuteAsync("usp_CreateMachining", machiningMasterDto, commandType: CommandType.StoredProcedure);

                // Start machining after creating it
                await connection.ExecuteAsync("usp_StartMachining", parameters, commandType: CommandType.StoredProcedure);
                return _userMessageService.GetMessage(1031);
            }
            else
            {
                // Start machining if it already exists
                await connection.ExecuteAsync("usp_StartMachining", parameters, commandType: CommandType.StoredProcedure);
                return _userMessageService.GetMessage(1082);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error starting machining: {ex.Message}", ex);
        }
    }
    public async Task TogglePauseAsync(string machiningId, string pauseCode)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "usp_ToggleMachiningPause",
            new { MachiningID = machiningId, PauseCode = pauseCode },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<bool> EndMachiningAsync(string machiningId)
    {
        using var connection = CreateConnection(); // Use the same connection factory
        var parameters = new { MachiningID = machiningId };

        // 1. Get current status
        var machiningInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "usp_GetMachiningStatusAndOperator",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        string status = machiningInfo?.MachiningStatus;

        // 2. If paused, toggle resume
        if (status == "Machining Pause")
        {
            await connection.ExecuteAsync("usp_ToggleMachiningPause", parameters, commandType: CommandType.StoredProcedure);
        }

        // 3. End machining
        var rowsAffected = await connection.ExecuteAsync("usp_EndMachining", parameters, commandType: CommandType.StoredProcedure);

        // 4. Update end time
        if (rowsAffected > 0)
        {
            await connection.ExecuteAsync("usp_UpdateMachiningEndTime",
                new { EndTime = DateTime.Now, MachiningID = machiningId },
                commandType: CommandType.StoredProcedure);
            return true;
        }

        return false;
    }
    public async Task AddQuantitiesAsync(string machiningId, int totalQty, int processedQty, string qtyStatus)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "usp_AddQuantities",
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
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "usp_AddMachiningDelays",
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
        using var connection = CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<MachiningMaster>(
            "dbo.usp_GetMachiningByCompositeKey",
            new { WorkCenterNo = workCenterNo, WorkOrderNo = workOrderNo, OperationNo = operationNo },
            commandType: CommandType.StoredProcedure
        );
        return result;
    }

    public async Task UpdateMachiningStatusAsync(string machiningId)
    {
        using var connection = CreateConnection();

        await connection.ExecuteAsync(
            "usp_UpdateMachiningStatus",
            new { MachiningId = machiningId },
            commandType: CommandType.StoredProcedure
        );
    }
}

