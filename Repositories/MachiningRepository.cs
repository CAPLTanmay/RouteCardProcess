using System.Data;
using Azure.Core;
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

        TimeSpan StandardMachiningTime;
        if (TimeSpan.TryParse(obj.StandardMachiningTime, out var tsValue))
        {
            StandardMachiningTime = tsValue;
        }
        else if (double.TryParse(obj.StandardMachiningTime, out var mins))
        {
            StandardMachiningTime = TimeSpan.FromMinutes(mins);
        }
        else
        {
            StandardMachiningTime = TimeSpan.Zero;
        }

        var parameters = new
        {
            MachiningId = machiningId,
            OperatorId = obj.OperatorId,
            WorkCenterNo = obj.WorkCenterNo,
            DepartmentId = obj.DepartmentId,
            ProductionOrderNo = obj.ProductionOrderNo,
            OperationNo = obj.OperationNo,
            StandardMachiningTime = StandardMachiningTime
        };

        using var multi = await connection.QueryMultipleAsync("usp_CreateMachining", parameters, commandType: CommandType.StoredProcedure);

        //  FIRST result set: Trans_Machining + Operator
        var machiningData = await multi.ReadFirstOrDefaultAsync<MachiningMaster>();

        //  SECOND result set: SAP Routing Info
        var sapInfo = await multi.ReadFirstOrDefaultAsync<SapRoutingInfo>();

        if (machiningData != null && sapInfo != null)
        {
            machiningData.TotalQty = sapInfo.TotalQty;
            machiningData.S_ConfirmedQuantity = sapInfo.S_ConfirmedQuantity;
            machiningData.L_CompletedQty = sapInfo.L_CompletedQty;
            machiningData.PendingQty = sapInfo.PendingQty;
            machiningData.OrderTypeDesc = sapInfo.OrderTypeDesc;
        }

        return machiningData;

    }

    public async Task InsertMachiningOperatorStartAsync(MachiningOperatorStartRequest request)
    {
        var query = "EXEC Insert_MachiningOperatorStart @MachiningId, @OperatorId, @OperatorStartTime";

        var parameters = new
        {
            MachiningId = request.MachiningId,
            OperatorId = request.OperatorId,
            OperatorStartTime = request.OperatorStartTime
        };

        using var connection = CreateConnection();
        await connection.ExecuteAsync(query, parameters);
    }


    public async Task<string> StartMachiningAsync(MachiningIdentifierRequest request)
    {
        using var connection = CreateConnection(); 
        var parameters = new { MachiningID = request.MachiningId };

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
                    MachiningId = request.MachiningId,
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
    public async Task TogglePauseAsync(MachiningPauseRequest request)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "usp_ToggleMachiningPause",
            new { MachiningID = request.MachiningId, PauseCode = request.PauseCode },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<bool> EndMachiningAsync(MachiningIdentifierRequest request)
    {
        using var connection = CreateConnection(); // Use the same connection factory
        var parameters = new { MachiningID = request.MachiningId };

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
                new { EndTime = DateTime.Now, MachiningID = request.MachiningId },
                commandType: CommandType.StoredProcedure);
            return true;
        }
        return false;
    }
    public async Task AddQuantitiesAsync(AddQuantityRequest request)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "usp_AddQuantities",
            new
            {
                MachiningID = request.MachiningId,
                TotalQty = request.TotalQty,
                ProcessedQty = request.ProcessedQty,
                QtyStatus = request.QtyStatus
            },
            commandType: CommandType.StoredProcedure
        );
    }
    public async Task<ProcessQuantityResponse> ProcessQuantitiesAsync(AddQuantity request)
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

            int totalProcessed = totalCompleted + totalHandover;

            //  Get PendingQty from DB
            int pendingQty = await connection.QueryFirstOrDefaultAsync<int>(
                "usp_GetPendingQty",
                new { MachiningID = request.MachiningId },
                transaction,
                commandType: CommandType.StoredProcedure
            );

            // Validate before insert
            if (totalProcessed > pendingQty)
            {
                transaction.Rollback(); // Still rollback transaction
                return new ProcessQuantityResponse
                {
                    Success = false,
                    Message = $"Processed quantity ({totalProcessed}) exceeds pending quantity ({pendingQty})."
                };
            }

            // string newStatus = "Completed";

            //if (totalCompleted < pendingQty)
            //{
            //   newStatus = "Handover";
            //}

            //string newStatus = totalHandover > 0 ? "Handover" : "Partially Completed";

            string newStatus = (pendingQty == totalCompleted)? "Completed": (totalHandover > 0 ? "Handover" : "Partially Completed");


            // Insert into DB
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

            return new ProcessQuantityResponse
            {
                Success = true,
                Message = "Quantities processed successfully."
            };
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return new ProcessQuantityResponse
            {
                Success = false,
                Message = "An error occurred: " + ex.Message
            };
        }
    }

    public async Task<bool> AddDelaysAsync(MachiningDelayRequest request)
    {
        using var connection = (SqlConnection)CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Get operator info once
            var machining = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "usp_GetMachiningStatusAndOperator",
                new { MachiningID = request.MachiningId },
                transaction,
                commandType: CommandType.StoredProcedure
            );

            if (machining == null)
            {
                transaction.Rollback();
                return false;
            }

            // Insert Exceptions if any
            if (request.Exceptions?.Any() == true)
            {
                foreach (var exception in request.Exceptions)
                {
                    await connection.ExecuteAsync(
                        "usp_InsertMachiningException",
                        new
                        {
                            MachiningID = request.MachiningId,
                            OperatorId = machining.OperatorId,
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
                        "usp_InsertMachiningIdle",
                        new
                        {
                            MachiningID = request.MachiningId,
                            OperatorId = machining.OperatorId,
                            idle.MSTIdleCode,
                            idle.MachiningIdleTime
                        },
                        transaction,
                        commandType: CommandType.StoredProcedure
                    );
                }
            }     
            transaction.Commit();
            return true;
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }
    public async Task<MachiningMaster> GetByCompositeKeyAsync(CompositeKeyRequest request)
    {
        using var connection = CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<MachiningMaster>(
            "dbo.usp_GetMachiningByCompositeKey",
            new {
                WorkCenterNo = request.WorkCenterNo,
                ProductionOrderNo = request.ProductionOrderNo,
                OperationNo = request.OperationNo
            },

            commandType: CommandType.StoredProcedure
        );
        return result;
    }
    public async Task UpdateMachiningStatusAsync(MachiningIdentifierRequest request)
    {
        using var connection = CreateConnection();

        await connection.ExecuteAsync(
            "usp_UpdateMachiningStatus",
            new { MachiningId = request.MachiningId },
            commandType: CommandType.StoredProcedure
        );
    }
}

