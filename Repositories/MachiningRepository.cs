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
    public async Task<MachiningStartResponse> StartMachiningAsync(MachiningIdentifierRequest request)
    {
        using var connection = CreateConnection();

        try
        {
            // Step 1: Check if machining exists
            var existingMachining = await connection.QueryFirstOrDefaultAsync<int?>(
                "usp_CheckMachiningExists",
                new { MachiningID = request.MachiningId },
                commandType: CommandType.StoredProcedure
            );

            if (existingMachining == null)
            {
                var machiningMasterDto = new MachiningDto
                {
                    MachiningId = request.MachiningId,
                    // Populate other necessary properties
                };

                await connection.ExecuteAsync(
                    "usp_CreateMachining",
                    machiningMasterDto,
                    commandType: CommandType.StoredProcedure
                );
            }

            // Step 2: Call usp_StartMachining with OUTPUT parameter
            var parameters = new DynamicParameters();
            parameters.Add("@MachiningID", request.MachiningId);
            parameters.Add("@OperatorStartTime", dbType: DbType.DateTime, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "usp_StartMachining",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var operatorStartTime = parameters.Get<DateTime>("@OperatorStartTime");

            var message = existingMachining == null
                ? _userMessageService.GetMessage(1031)  // "Machining created and started"
                : _userMessageService.GetMessage(1082); // "Machining started"

            return new MachiningStartResponse
            {
                Message = message,
                StartDate = operatorStartTime.ToString("yyyy-MM-dd"),
                StartTime = operatorStartTime.ToString("HH:mm:ss")
            };
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

    public async Task<EndMachiningResultDto> EndMachiningAsync(MachiningIdentifierRequest request)
    {
        using var connection = CreateConnection();
        var parameters = new { MachiningID = request.MachiningId };

        // 1. Get machining info (status + standard time)
        var machiningInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "usp_GetMachiningStatusAndOperator",
            parameters,
            commandType: CommandType.StoredProcedure);

        string status = machiningInfo?.MachiningStatus;
        string standardMachiningTime = machiningInfo?.StandardMachiningTime != null
            ? TimeSpan.Parse(machiningInfo.StandardMachiningTime.ToString()).ToString(@"hh\:mm\:ss")
            : null;

        // 2. If paused, resume it
        if (status == _userMessageService.GetMessage(1076)) // assuming 1076 = "Machining Pause"
        {
            await connection.ExecuteAsync("usp_ToggleMachiningPause", parameters, commandType: CommandType.StoredProcedure);
        }

        // 3. End machining
        var rowsAffected = await connection.ExecuteAsync("usp_EndMachining", parameters, commandType: CommandType.StoredProcedure);
        // 4. If successful, get the time difference
        if (rowsAffected > 0)
        {
            var machiningData = await connection.QueryFirstOrDefaultAsync<dynamic>( "usp_GetMachiningTimeDetails", parameters,commandType: CommandType.StoredProcedure);


            string machiningTimeDiff = null;
            string machiningStartTimeStr = null;
            string machiningEndTimeStr = null;

            if (machiningData != null && machiningData.MachiningStartTime != null && machiningData.MachiningEndTime != null)
            {
                DateTime startTime = machiningData.MachiningStartTime;
                DateTime endTime = machiningData.MachiningEndTime;

                TimeSpan diff = endTime - startTime;
                machiningTimeDiff = diff.ToString(@"hh\:mm\:ss");

                // Format start and end times in HH:mm:ss
                machiningStartTimeStr = startTime.ToString("HH:mm:ss");
                machiningEndTimeStr = endTime.ToString("HH:mm:ss");
            }

            return new EndMachiningResultDto
            {
                Success = true,
                Message = _userMessageService.GetMessage(1027),
                MachiningTimeDiff = machiningTimeDiff,
                StandardMachiningTime = standardMachiningTime,
                MachiningStartTime = machiningStartTimeStr,
                MachiningEndTime = machiningEndTimeStr
            };
        }


        return new EndMachiningResultDto
        {
            Success = false,
            Message = _userMessageService.GetMessage(1087),
            MachiningTimeDiff = null
        };
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

