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
        ts.SetupStartTime,
        ts.SetupEndTime,
        DATEDIFF(MINUTE, ts.SetupStartTime, ts.SetupEndTime) AS ActualSetupTime,
        ISNULL(tsi.TotalSetupIdleMinutes, 0) AS TotalSetupIdleMinutes,
        RIGHT('0' + CAST(tsi.TotalSetupIdleMinutes / 60 AS VARCHAR), 2) + ':' +
        RIGHT('0' + CAST(tsi.TotalSetupIdleMinutes % 60 AS VARCHAR), 2) + ':00' AS TotalSetupIdle_HHMMSS,
        ISNULL(tse.TotalSetupExceptionsMinutes, 0) AS TotalSetupExceptionsMinutes,
        RIGHT('0' + CAST(tse.TotalSetupExceptionsMinutes / 60 AS VARCHAR), 2) + ':' +
        RIGHT('0' + CAST(tse.TotalSetupExceptionsMinutes % 60 AS VARCHAR), 2) + ':00' AS TotalSetupExceptions_HHMMSS,
        tso.OperatorStartTime AS SetupOperatorStartTime,
        tso.OperatorEndTime AS SetupOperatorEndTime,
	    tm.MachiningId,
        tm.MachiningStartTime,
        tm.MachiningEndTime,
        DATEDIFF(MINUTE, tm.MachiningStartTime,   tm.MachiningEndTime) AS ActualMachiningTime,
        ISNULL(tmi.TotalMachiningIdleMinutes, 0) AS TotalMachiningIdleMinutes,
        RIGHT('0' + CAST(tmi.TotalMachiningIdleMinutes / 60 AS VARCHAR), 2) + ':' +
        RIGHT('0' + CAST(tmi.TotalMachiningIdleMinutes % 60 AS VARCHAR), 2) + ':00' AS TotalMachiningIdle_HHMMSS,
        ISNULL(tme.TotalMachiningExceptionsMinutes, 0) AS TotalMachiningExceptionsMinutes,
        RIGHT('0' + CAST(tme.TotalMachiningExceptionsMinutes / 60 AS VARCHAR), 2) + ':' +
        RIGHT('0' + CAST(tme.TotalMachiningExceptionsMinutes % 60 AS VARCHAR), 2) + ':00' AS TotalMachiningExceptions_HHMMSS,
        tmo.OperatorStartTime AS MachiningOperatorStartTime,
        tmo.OperatorEndTime AS MachiningOperatorEndTime,
DATEDIFF(MINUTE, ts.SetupStartTime, ts.SetupEndTime) 
+ DATEDIFF(MINUTE, tm.MachiningStartTime, tm.MachiningEndTime) AS ActualOperationTime,
ISNULL(tsi.TotalSetupIdleMinutes, 0) + ISNULL(tmi.TotalMachiningIdleMinutes, 0) AS IdleOperationTime,
DATEDIFF(MINUTE, tso.OperatorStartTime, tso.OperatorEndTime) AS SetupLaborTime,
DATEDIFF(MINUTE, tmo.OperatorStartTime, tmo.OperatorEndTime) AS MachiningLaborTime,
DATEDIFF(MINUTE, tso.OperatorStartTime, tso.OperatorEndTime)
+ DATEDIFF(MINUTE, tmo.OperatorStartTime, tmo.OperatorEndTime) AS ActualLaborTime,
CAST(
    (DATEDIFF(MINUTE, tso.OperatorStartTime, tso.OperatorEndTime)
   + DATEDIFF(MINUTE, tmo.OperatorStartTime, tmo.OperatorEndTime)) / 60.0
AS DECIMAL(10, 2)) AS ActualLaborTime_Hours,


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
    INNER JOIN Trans_Machining tm ON tm.ProductionOrderNo = ts.ProductionOrderNo
        AND tm.OperationNo = ts.OperationNo
        AND tm.MachiningStartTime >= ts.SetupEndTime
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


        public async Task<LossOrderResponseDto> GetLossOrderByIdsAsync(string? setupId, string? machiningId)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            List<SetupIdleDto> setupData = new();
            List<MachiningIdleDto> machData = new();

            // Fetch Setup idle records
            if (!string.IsNullOrEmpty(setupId))
            {
                var setupQuery = @"
            SELECT 
                tsi.SetUpID,
                tsi.OperatorId,
                tsi.LossOrderNumber AS [ORDER],
                tsi.MSTIdleCode,
                tsi.SetupIdleTime
            FROM Trans_Setup_IdelTime tsi
            WHERE tsi.SetupId = @SetupId";

                setupData = (await connection.QueryAsync<SetupIdleDto>(setupQuery, new { SetupId = setupId })).ToList();
            }

            // Fetch Machining idle records
            if (!string.IsNullOrEmpty(machiningId))
            {
                var machQuery = @"
            SELECT 
                tmi.MachiningID,
                tmi.OperatorId,
                tmi.LossOrderNumber AS [ORDER],
                tmi.MSTIdleCode,
                tmi.MachiningIdleTime
            FROM Trans_Machining_IdelTime tmi
            WHERE tmi.MachiningId = @MachiningId";

                machData = (await connection.QueryAsync<MachiningIdleDto>(machQuery, new { MachiningId = machiningId })).ToList();
            }

            // Return null if no data
            if (!setupData.Any() && !machData.Any())
                return null;

            // Get order number from first available record
            var orderNo = setupData.FirstOrDefault()?.ORDER ?? machData.FirstOrDefault()?.ORDER;

            return new LossOrderResponseDto
            {
                ORDER = orderNo,
                SetupIdleRecords = setupData,
                MachiningIdleRecords = machData
            };
        }



    }
}