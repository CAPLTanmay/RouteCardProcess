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

            var sql = @"
            -- Final query starts here
            SELECT
                lm.OperatorName,
                sm.ShiftCode AS CurrentShift,
                tso.OperatorId,
                ts.ProductionOrderNo,
                ts.WorkCenterNo,
	            srd.WorkCenterText,
                srd.Material,
                srd.MaterialText,
                srd.MrpController,
	            srd.ProductionScheduler,
	            srd.ProcessingUnit,
	            srd.ProductionUnit,
                ts.OperationNo,
	            srd.OperationDescription,
                srd.OrderType,
                srd.TotalQty,
                (srd.TotalQty - srd.S_ConfirmedQuantity) AS Pending_qty,
	            tmo.CompletedQty,
                ts.SetupId,
                CONVERT(DATE, ts.SetupStartTime) AS SetupStartDate,
                CONVERT(TIME, ts.SetupStartTime) AS SetupStartTime,

                CONVERT(DATE, ts.SetupEndTime) AS SetupEndDate,
                CONVERT(TIME, ts.SetupEndTime) AS SetupEndTime,

                
                ts.StandardSetupTime,
                DATEDIFF(MINUTE, ts.SetupStartTime, ts.SetupEndTime) AS ActualSetupTime,
            -- Convert total minutes to HH:MM:SS format
                RIGHT('0' + CAST((DATEDIFF(MINUTE, ts.SetupStartTime, ts.SetupEndTime) / 60) AS VARCHAR), 2) + ':' +
                RIGHT('0' + CAST((DATEDIFF(MINUTE, ts.SetupStartTime, ts.SetupEndTime) % 60) AS VARCHAR), 2) + ':00'
                AS ActualSetupTime_HHMMSS,

                ISNULL(tsi.TotalSetupIdleMinutes, 0) AS TotalSetupIdleMinutes,
                RIGHT('0' + CAST(tsi.TotalSetupIdleMinutes / 60 AS VARCHAR), 2) + ':' +
                RIGHT('0' + CAST(tsi.TotalSetupIdleMinutes % 60 AS VARCHAR), 2) + ':00' AS TotalSetupIdle_HHMMSS,
                ISNULL(tse.TotalSetupExceptionsMinutes, 0) AS TotalSetupExceptionsMinutes,
                RIGHT('0' + CAST(tse.TotalSetupExceptionsMinutes / 60 AS VARCHAR), 2) + ':' +
                RIGHT('0' + CAST(tse.TotalSetupExceptionsMinutes % 60 AS VARCHAR), 2) + ':00' AS TotalSetupExceptions_HHMMSS,
                tso.OperatorStartTime AS SetupOperatorStartTime,
                tso.OperatorEndTime AS SetupOperatorEndTime,
	            tm.MachiningId,
              
               

                CONVERT(DATE,  tm.MachiningStartTime) AS MachiningStartDate,
                CONVERT(TIME,  tm.MachiningStartTime) AS MachiningStartTime,

                CONVERT(DATE, tm.MachiningEndTime) AS MachiningEndDate,
                CONVERT(TIME,  tm.MachiningEndTime) AS MachiningEndTime,

                tm.StandardMachiningTime,
               DATEDIFF(MINUTE, tm.MachiningStartTime, tm.MachiningEndTime) AS ActualMachiningTime,
RIGHT('0' + CAST((DATEDIFF(MINUTE, tm.MachiningStartTime, tm.MachiningEndTime) / 60) AS VARCHAR), 2) + ':' +
RIGHT('0' + CAST((DATEDIFF(MINUTE, tm.MachiningStartTime, tm.MachiningEndTime) % 60) AS VARCHAR), 2) + ':00'
AS ActualMachiningTime_HHMMSS,


                ISNULL(tmi.TotalMachiningIdleMinutes, 0) AS TotalMachiningIdleMinutes,
                RIGHT('0' + CAST(tmi.TotalMachiningIdleMinutes / 60 AS VARCHAR), 2) + ':' +
                RIGHT('0' + CAST(tmi.TotalMachiningIdleMinutes % 60 AS VARCHAR), 2) + ':00' AS TotalMachiningIdle_HHMMSS,
                ISNULL(tme.TotalMachiningExceptionsMinutes, 0) AS TotalMachiningExceptionsMinutes,
                RIGHT('0' + CAST(tme.TotalMachiningExceptionsMinutes / 60 AS VARCHAR), 2) + ':' +
                RIGHT('0' + CAST(tme.TotalMachiningExceptionsMinutes % 60 AS VARCHAR), 2) + ':00' AS TotalMachiningExceptions_HHMMSS,
                tmo.OperatorStartTime AS MachiningOperatorStartTime,
                tmo.OperatorEndTime AS MachiningOperatorEndTime,
                DATEDIFF(MINUTE, ts.SetupStartTime, ts.SetupEndTime) + DATEDIFF(MINUTE, tm.MachiningStartTime, tm.MachiningEndTime) AS ActualOperationTime,
                ISNULL(tsi.TotalSetupIdleMinutes, 0) + ISNULL(tmi.TotalMachiningIdleMinutes, 0) AS IdleOperationTime,
                ISNULL(tse.TotalSetupExceptionsMinutes, 0) + ISNULL(tme.TotalMachiningExceptionsMinutes, 0) AS ExceptionOperationTime,
                DATEDIFF(MINUTE, tso.OperatorStartTime, tso.OperatorEndTime) AS SetupLaborTime,
                DATEDIFF(MINUTE, tmo.OperatorStartTime, tmo.OperatorEndTime) AS MachiningLaborTime,
                DATEDIFF(MINUTE, tso.OperatorStartTime, tso.OperatorEndTime) + DATEDIFF(MINUTE, tmo.OperatorStartTime, tmo.OperatorEndTime) AS ActualLaborTime,
               CAST( (DATEDIFF(MINUTE, tso.OperatorStartTime, tso.OperatorEndTime) + DATEDIFF(MINUTE, tmo.OperatorStartTime, tmo.OperatorEndTime)) / 60.0 AS DECIMAL(10, 2)) AS ActualLaborTime_Hours,


               CAST(tm.MachiningEndTime AS DATE) AS FinishDate

            FROM Trans_Setup ts

            INNER JOIN Trans_Setup_Operator tso ON ts.SetupId = tso.SetupId
            LEFT JOIN (
                SELECT SetupId, SUM(DATEDIFF(MINUTE, '00:00:00', SetupIdleTime)) AS TotalSetupIdleMinutes
                FROM Trans_Setup_IdelTime GROUP BY SetupId
            ) tsi ON ts.SetupId = tsi.SetupId

            LEFT JOIN (
                SELECT SetupId, SUM(DATEDIFF(MINUTE, '00:00:00', ExceptionsTime)) AS TotalSetupExceptionsMinutes
                FROM Trans_Setup_ExceptionsTime GROUP BY SetupId
            ) tse ON ts.SetupId = tse.SetupId

            INNER JOIN LogInMaster lm ON tso.OperatorId = lm.OperatorId
            LEFT JOIN ShiftMaster sm 
                ON (
                    (sm.StartTime < sm.EndTime AND CAST(GETDATE() AS TIME) BETWEEN sm.StartTime AND sm.EndTime)
                    OR (sm.StartTime > sm.EndTime AND (CAST(GETDATE() AS TIME) >= sm.StartTime OR CAST(GETDATE() AS TIME) <= sm.EndTime))
                )

           INNER JOIN Trans_Machining tm ON tm.SetupId = ts.SetupId

            INNER JOIN Trans_Machining_Operator tmo ON tm.MachiningId = tmo.MachiningId
            LEFT JOIN (
                SELECT MachiningId, SUM(DATEDIFF(MINUTE, '00:00:00', MachiningIdleTime)) AS TotalMachiningIdleMinutes
                FROM Trans_Machining_IdelTime GROUP BY MachiningId
            ) tmi ON tm.MachiningId = tmi.MachiningId
            LEFT JOIN (
                SELECT MachiningId, SUM(DATEDIFF(MINUTE, '00:00:00', ExceptionsTime)) AS TotalMachiningExceptionsMinutes
                FROM Trans_Machining_ExceptionsTime GROUP BY MachiningId
            ) tme ON tm.MachiningId = tme.MachiningId
            INNER JOIN SapRoutingData srd ON ts.ProductionOrderNo = srd.WorkOrder AND ts.OperationNo = srd.OperationNo

            WHERE 
                (@OperatorId IS NULL OR tso.OperatorId = @OperatorId)
                AND (@ConfirmationDate IS NULL OR CAST(tm.MachiningEndTime AS DATE) = @ConfirmationDate)
                AND (@ProductionOrderNo IS NULL OR ts.ProductionOrderNo = @ProductionOrderNo)
    
                AND (@WorkCenterNo IS NULL OR ts.WorkCenterNo = @WorkCenterNo)
            ";

            var result = await connection.QueryAsync<RouteCardReportDto>(sql, request);
            return result;
        }

        public async Task<LossOrderResponseDto> GetLossOrderByIdsAsync(OrderReportRequestDto request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var parameters = new
            {
                SetupId = request.SetupId,
                MachiningId = request.MachiningId
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
                MachiningId = request.MachiningId
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

            if (!string.IsNullOrEmpty(request.MachiningId))
            {
                var machiningQuery = @"
                    SELECT 
                        m.StandardMachiningTime,
                        CONVERT(DATE, m.MachiningStartTime) AS MachiningStartDate,
                        CONVERT(TIME, m.MachiningStartTime) AS MachiningStartTime,
                        CONVERT(DATE, m.MachiningEndTime) AS MachiningEndDate,
                        CONVERT(TIME, m.MachiningEndTime) AS MachiningEndTime,
                        m.TotalMachiningTime,
                        mo.CompletedQty
                    FROM Trans_Machining m
                    LEFT JOIN Trans_Machining_Operator mo ON m.MachiningId = mo.MachiningId
                    WHERE m.MachiningId = @MachiningId";

                var machiningResult = await connection.QueryFirstOrDefaultAsync<TimingInfoDto>(machiningQuery, new { request.MachiningId });
                if (machiningResult != null)
                {
                    timingInfo.StandardMachiningTime = machiningResult.StandardMachiningTime;
                    timingInfo.MachiningStartDate = machiningResult.MachiningStartDate;
                    timingInfo.MachiningStartTime = machiningResult.MachiningStartTime;
                    timingInfo.MachiningEndDate = machiningResult.MachiningEndDate;
                    timingInfo.MachiningEndTime = machiningResult.MachiningEndTime;
                    timingInfo.TotalMachiningTime = machiningResult.TotalMachiningTime;
                    timingInfo.CompletedQty = machiningResult.CompletedQty;
                }
            }

            if (!string.IsNullOrEmpty(request.SetupId))
            {
                var setupQuery = @"
            SELECT 
                StandardSetupTime,
                CONVERT(DATE, SetupStartTime) AS SetupStartDate,
                CONVERT(TIME, SetupStartTime) AS SetupStartTime,
                CONVERT(DATE, SetupEndTime) AS SetupEndDate,
                CONVERT(TIME, SetupEndTime) AS SetupEndTime,
                TotalSetupTime
            FROM Trans_Setup 
            WHERE SetupId = @SetupId";

                var setupResult = await connection.QueryFirstOrDefaultAsync<TimingInfoDto>(setupQuery, new { request.SetupId });
                if (setupResult != null)
                {
                    timingInfo.StandardSetupTime = setupResult.StandardSetupTime;
                    timingInfo.SetupStartDate = setupResult.SetupStartDate;
                    timingInfo.SetupStartTime = setupResult.SetupStartTime;
                    timingInfo.SetupEndDate = setupResult.SetupEndDate;
                    timingInfo.SetupEndTime = setupResult.SetupEndTime;
                    timingInfo.TotalSetupTime = setupResult.TotalSetupTime;
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
                SetupStartTime = dto.SetupStartDateTime,   
                SetupEndTime = dto.SetupEndDateTime,
                dto.UpdatedOperatorId
            }, commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateIdleTimesAsync(string setupId, int operatorId, List<IdleTimeUpdateDto> idleTimes)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            foreach (var idle in idleTimes)
            {
                await connection.ExecuteAsync("dbo.usp_UpdateIdleTimes", new
                {
                    SetUpID = setupId,
                    idle.MSTIdleCode,
                    idle.NewSetupIdleTime,
                    UpdatedOperatorId = operatorId
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task UpdateExceptionTimesAsync(string setupId, int operatorId, List<ExceptionTimeUpdateDto> exceptions)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            foreach (var ex in exceptions)
            {
                await connection.ExecuteAsync("dbo.usp_UpdateExceptionTimes", new
                {
                    SetUpID = setupId,
                    ex.StdExceptionsReasonCode,
                    ex.ExceptionsReasonCode,
                    ex.NewExceptionsTime,
                    UpdatedOperatorId = operatorId
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task UpdateMachiningTimesAsync(MachiningUpdateDto dto)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await connection.ExecuteAsync("dbo.usp_UpdateMachiningTimes", new
            {
                dto.MachiningId,
                MachiningStartTime = dto.MachiningStartDateTime,
                MachiningEndTime = dto.MachiningEndDateTime,     
                dto.UpdatedOperatorId
            }, commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateMachiningIdleTimesAsync(string machiningId, int operatorId, List<MachiningIdleTimeUpdateDto> idleTimes)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            foreach (var idle in idleTimes)
            {
                await connection.ExecuteAsync("dbo.usp_UpdateMachiningIdleTimes", new
                {
                    MachiningId = machiningId,
                    idle.MSTIdleCode,
                    idle.NewMachiningIdleTime,
                    UpdatedOperatorId = operatorId
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task UpdateMachiningExceptionTimesAsync(string machiningId, int operatorId, List<MachiningExceptionUpdateDto> exceptions)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            foreach (var ex in exceptions)
            {
                await connection.ExecuteAsync("dbo.usp_UpdateMachiningExceptionTimes", new
                {
                    MachiningID = machiningId,
                    ex.StdExceptionsReasonCode,
                    ex.ExceptionsReasonCode,
                    ex.NewExceptionsTime,
                    UpdatedByOperatorId = operatorId
                }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task UpdateMachiningOperatorQuantitiesAsync(string machiningId, List<MachiningOperatorQtyUpdateDto> quantities)
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