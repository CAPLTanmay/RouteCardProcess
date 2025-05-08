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

    public async Task StartMachiningAsync(string machiningId)
    {
        using var connection = Connection;
        await connection.ExecuteAsync(
            "sp_StartMachining",
            new { MachiningID = machiningId },
            commandType: CommandType.StoredProcedure
        );
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

    public async Task EndMachiningAsync(string machiningId)
    {
        using var connection = Connection;
        await connection.ExecuteAsync(
            "sp_EndMachining",
            new { MachiningID = machiningId },
            commandType: CommandType.StoredProcedure
        );
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
