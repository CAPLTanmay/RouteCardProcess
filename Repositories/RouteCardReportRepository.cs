using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.RouteCardReport;

namespace RouteCardProcess.Repositories
{
    public class RouteCardReportRepository : IRouteCardReportRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ILogInRepository _logInRepository;

        public RouteCardReportRepository(SqlConnectionFactory connectionFactory, ILogInRepository logInRepository)
        {
            _connectionFactory = connectionFactory;
            _logInRepository = logInRepository;
        }

        public async Task<IEnumerable<RouteCardReportModel>> GetRouteCardReportAsync(string workOrderNo)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var result = (await connection.QueryAsync<RouteCardReportModel>(
                "usp_GetRouteCardReport",
                new { WorkOrderNo = workOrderNo },
                commandType: CommandType.StoredProcedure)).AsList();

            for (int i = 0; i < result.Count; i++)
            {
                DateTime shiftTime;
                if (!string.IsNullOrWhiteSpace(result[i].MachiningEndTime) &&
                    DateTime.TryParse(result[i].MachiningEndTime, out shiftTime))
                {
                    result[i].Shift = await _logInRepository.GetCurrentShiftAsync(shiftTime);
                }
                else
                {
                    result[i].Shift = await _logInRepository.GetCurrentShiftAsync(DateTime.Now);
                }
            }

            return result;
        }

        public async Task<IEnumerable<RouteCardReportDto>> GetRouteCardReportFilteredAsync(RouteCardReportFilterRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            //  Pad ProductionOrderNo before using it in the query
            var paddedOrderNo = request.ProductionOrderNo?.PadLeft(12, '0');

            var result = await connection.QueryAsync<RouteCardReportDto>(
                "usp_GetRouteCardReportFiltered",
                new
                {
                    request.OperatorId,
                    request.ConfirmationDate,
                    ProductionOrderNo = paddedOrderNo,
                    request.WorkCenterNo,
                    Dept = request.Department
                },
                commandType: CommandType.StoredProcedure);

            return result;
        }

        public async Task<IEnumerable<RouteCardReportDto>> GetRouteCardReportAllAsync(RouteCardReportFilterRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            //  Pad ProductionOrderNo before using it in the query
            var paddedOrderNo = request.ProductionOrderNo?.PadLeft(12, '0');

            var result = await connection.QueryAsync<RouteCardReportDto>(
                "usp_GetRouteCardReportAll",
                new
                {
                    request.OperatorId,
                    request.ConfirmationDate,
                    ProductionOrderNo = paddedOrderNo,
                    request.WorkCenterNo,
                    Dept = request.Department
                },
                commandType: CommandType.StoredProcedure);

            return result;
        }

        public async Task<LossOrderResponseDto> GetLossOrderByIdsAsync(OrderReportRequestDto request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var parameters = new
            {
                SetupId = request.SetupId,
                MachiningId = request.MachiningId,
                OperatorId = request.OperatorId
            };

            using var multi = await connection.QueryMultipleAsync("dbo.usp_GetLossOrderByIds", parameters, commandType: CommandType.StoredProcedure);

            var setupData = (await multi.ReadAsync<SetupIdleDto>()).ToList();
            var machData = (await multi.ReadAsync<MachiningIdleDto>()).ToList();

            if (!setupData.Any() && !machData.Any())
                return null;

            var orderNo = setupData.FirstOrDefault()?.ORDER ?? machData.FirstOrDefault()?.ORDER;

            return new LossOrderResponseDto
            {
                ORDER = orderNo,
                SetupIdleRecords = setupData,
                MachiningIdleRecords = machData
            };
        }

        public async Task<ExceptionReportResponseDto?> GetExceptionReportAsync(OrderReportRequestDto request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var parameters = new
            {
                SetupId = request.SetupId,
                MachiningId = request.MachiningId,
                OperatorId = request.OperatorId
            };

            using var multi = await connection.QueryMultipleAsync("dbo.usp_GetExceptionReport", parameters, commandType: CommandType.StoredProcedure);

            var setupExceptions = (await multi.ReadAsync<ExceptionRecordDto>()).ToList();
            var machiningExceptions = (await multi.ReadAsync<ExceptionRecordDto>()).ToList();

            if (!setupExceptions.Any() && !machiningExceptions.Any())
                return null;

            return new ExceptionReportResponseDto
            {
                SetupId = request.SetupId,
                MachiningId = request.MachiningId,
                SetupExceptions = setupExceptions,
                MachiningExceptions = machiningExceptions
            };
        }

        public async Task<TimingInfoDto?> GetTimingInfoAsync(OrderReportRequestDto request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var timingInfo = new TimingInfoDto();

            if (!string.IsNullOrEmpty(request.MachiningId) ||
                    (!request.MachiningOperatorTransactionId.HasValue || request.MachiningOperatorTransactionId == Guid.Empty))
            {
                var machiningResult = await connection.QueryFirstOrDefaultAsync<TimingInfoDto>(
                    "usp_GetMachiningTimingInfo",
                    new
                    {
                        MachiningId = request.MachiningId,
                        MachiningOperatorTransactionId = request.MachiningOperatorTransactionId
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (machiningResult != null)
                {
                    timingInfo.StandardMachiningTime = machiningResult.StandardMachiningTime;
                    timingInfo.MachiningStartDate = machiningResult.MachiningStartDate;
                    timingInfo.MachiningStartTime = machiningResult.MachiningStartTime;
                    timingInfo.MachiningEndDate = machiningResult.MachiningEndDate;
                    timingInfo.MachiningEndTime = machiningResult.MachiningEndTime;
                    timingInfo.TotalMachiningTime = machiningResult.TotalMachiningTime;
                    timingInfo.CompletedQty = machiningResult.CompletedQty;
                    timingInfo.MachiningId = request.MachiningId;

                    timingInfo.OperatorId = machiningResult.OperatorId;
                    timingInfo.OperatorStartDate = machiningResult.OperatorStartDate;
                    timingInfo.OperatorStartTime = machiningResult.OperatorStartTime;
                    timingInfo.OperatorEndTime = machiningResult.OperatorEndTime;
                    timingInfo.TotalOperatorTime = machiningResult.TotalOperatorTime;

                    timingInfo.MachiningOperatorTransactionId = request.MachiningOperatorTransactionId;
                }
            }

            if (!string.IsNullOrEmpty(request.SetupId) ||
               (!request.OperatorTransactionId.HasValue || request.OperatorTransactionId == Guid.Empty) )
            {
                var setupResult = await connection.QueryFirstOrDefaultAsync<TimingInfoDto>(
                    "usp_GetSetupTimingInfo",
                    new
                    {
                        SetupId = request.SetupId,
                        OperatorTransactionId = request.OperatorTransactionId
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (setupResult != null)
                {
                    timingInfo.StandardSetupTime = setupResult.StandardSetupTime;
                    timingInfo.SetupStartDate = setupResult.SetupStartDate;
                    timingInfo.SetupStartTime = setupResult.SetupStartTime;
                    timingInfo.SetupEndDate = setupResult.SetupEndDate;
                    timingInfo.SetupEndTime = setupResult.SetupEndTime;
                    timingInfo.TotalSetupTime = setupResult.TotalSetupTime;
                    timingInfo.SetupId = request.SetupId;

                    timingInfo.OperatorTransactionId = setupResult.OperatorTransactionId;
                    timingInfo.OperatorId = setupResult.OperatorId;
                    timingInfo.OperatorStartDate = setupResult.OperatorStartDate;
                    timingInfo.OperatorStartTime = setupResult.OperatorStartTime;
                    timingInfo.OperatorEndDate = setupResult.OperatorEndDate;
                    timingInfo.OperatorEndTime = setupResult.OperatorEndTime;
                    timingInfo.TotalOperatorTime = setupResult.TotalOperatorTime;
                }
            }

            return timingInfo;
        }


        public async Task UpdateSetupTimesAsync(SetupUpdateDto dto)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await connection.ExecuteAsync("dbo.usp_UpdateSetupTimes", new
            {
                dto.SetUpID,
                dto.OperatorTransactionId,
                SetupStartTime = dto.SetupStartDateTime,   
                SetupEndTime = dto.SetupEndDateTime,
                OperatorStartTime = dto.OperatorStartDateTime,
                OperatorEndTime = dto.OperatorEndDateTime,
                dto.UpdatedOperatorId
            }, commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateIdleTimesAsync(string OperatorId, string setupId, string UpdatedOperatorId, List<IdleTimeUpdateDto> idleTimes)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            foreach (var idle in idleTimes)
            {
                await connection.ExecuteAsync("dbo.usp_UpdateIdleTimes", new
                {
                    SetUpID = setupId,
                    OperatorId= OperatorId,
                    idle.MSTIdleCode,
                    idle.NewSetupIdleTime,
                    UpdatedOperatorId = UpdatedOperatorId
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task UpdateExceptionTimesAsync(string OperatorId, string setupId, String UpdatedOperatorId, List<ExceptionTimeUpdateDto> exceptions)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            foreach (var ex in exceptions)
            {
                await connection.ExecuteAsync("dbo.usp_UpdateExceptionTimes", new
                {
                    SetUpID = setupId,
                    OperatorId = OperatorId,
                    ex.StdExceptionsReasonCode,
                    ex.ExceptionsReasonCode,
                    ex.NewExceptionsTime,
                    UpdatedOperatorId = UpdatedOperatorId
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task UpdateMachiningTimesAsync(MachiningUpdateDto dto)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await connection.ExecuteAsync("dbo.usp_UpdateMachiningTimes", new
            {
                dto.MachiningOperatorTransactionId,
                dto.MachiningId,
                OperatorStartTime = dto.OperatorStartTime,
                OperatorEndTime = dto.OperatorEndTime,     
                dto.UpdatedOperatorId
            }, commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateMachiningIdleTimesAsync(string operatorId, string machiningId, string UpdatedOperatorId, List<MachiningIdleTimeUpdateDto> idleTimes)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            foreach (var idle in idleTimes)
            {
                await connection.ExecuteAsync("dbo.usp_UpdateMachiningIdleTimes", new
                {
                    OperatorId = operatorId,
                    MachiningId = machiningId,
                    idle.MSTIdleCode,
                    idle.NewMachiningIdleTime,
                    UpdatedOperatorId = UpdatedOperatorId
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task UpdateMachiningExceptionTimesAsync(string operatorId, string machiningId, string UpdatedOperatorId, List<MachiningExceptionUpdateDto> exceptions)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            foreach (var ex in exceptions)
            {
                await connection.ExecuteAsync("dbo.usp_UpdateMachiningExceptionTimes", new
                {
                    OperatorId = operatorId,
                    MachiningID = machiningId,
                    ex.StdExceptionsReasonCode,
                    ex.ExceptionsReasonCode,
                    ex.NewExceptionsTime,
                    UpdatedByOperatorId = UpdatedOperatorId
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task UpdateMachiningOperatorQuantitiesAsync( string machiningId, List<MachiningOperatorQtyUpdateDto> quantities)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            foreach (var qty in quantities)
            {
                await connection.ExecuteAsync("dbo.usp_UpdateMachiningOperatorQuantities", new
                {
                    MachiningId = machiningId,
                    qty.OperatorId,
                    qty.NewCompletedQty
                }, commandType: CommandType.StoredProcedure);
            }
        }

    }
}