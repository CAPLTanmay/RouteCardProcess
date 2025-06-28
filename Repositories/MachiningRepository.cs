using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
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
        var machiningId = Guid.NewGuid().ToString().Substring(0, 8);

        var result = await connection.QueryFirstOrDefaultAsync<MachiningMaster>(
            "usp_CreateMachining", 
            new
            {
                MachiningId = machiningId,
                OperatorId = obj.OperatorId,
                WorkCenterNo = obj.WorkCenterNo,
                DepartmentId=obj.DepartmentId,
                ProductionOrderNo = obj.ProductionOrderNo,
                OperationNo = obj.OperationNo,
                StandardMachiningTime = double.TryParse(obj.StandardMachiningTime, out var mins)
    ? TimeSpan.FromMinutes(mins)
    : TimeSpan.Zero

    },
            commandType: CommandType.StoredProcedure
        );

        return result;
    }


    public async Task<string> StartMachiningAsync(string machiningId)
    {
        using var connection = CreateConnection(); 
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

    public async Task ProcessQuantitiesAsync(AddQuantity request)
    {
        using var connection = (SqlConnection)CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            int totalCompleted = 0;
            int totalHandover = 0;

            foreach (var item in request.QuantityList)
            {
                var qty = int.Parse(item.ProcessedQty);

                if (item.MachiningStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    totalCompleted += qty;
                else if (item.MachiningStatus.Equals("Handover", StringComparison.OrdinalIgnoreCase))
                    totalHandover += qty;
            }

            // Decide status
            string newStatus = totalHandover > 0 ? "Handover" : "Completed";

            // Call SP once with everything
            await connection.ExecuteAsync(
                "usp_AddQuantities",
                new
                {
                    MachiningID = request.MachiningId,
                    CompletedQty = totalCompleted,
                    HandoverQty = totalHandover,
                    MachiningStatus = newStatus
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure
            );

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
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
    public async Task<MachiningMaster> GetByCompositeKeyAsync(string workCenterNo, string ProductionOrderNo, string operationNo)
    {
        using var connection = CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<MachiningMaster>(
            "dbo.usp_GetMachiningByCompositeKey",
            new { WorkCenterNo = workCenterNo, ProductionOrderNo = ProductionOrderNo, OperationNo = operationNo },
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

