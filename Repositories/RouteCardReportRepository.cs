using Dapper;
using Microsoft.Data.SqlClient;

namespace RouteCardProcess.Repositories
{
    public class RouteCardReportRepository
    {
        private readonly IConfiguration _config;
        private readonly LogInRepository _logInRepository;

        public RouteCardReportRepository(IConfiguration config, LogInRepository logInRepository)
        {
            _config = config;
            _logInRepository = logInRepository;
        }
        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        }
        public async Task<IEnumerable<RouteCardReportModel>> GetRouteCardReportAsync(string workOrderNo)
        {
            var sql = @"
SELECT 
    STM.WorkOrderNo,
    STM.SetUpID,
    STM.OperatorId AS Master_OperatorId,
    STM.WorkCenterNo,
    STM.OperationNo,
    CONVERT(VARCHAR(8), SDM.SetupStartTime, 108) AS SetupStartTime,
CONVERT(VARCHAR(8), SDM.SetupEndTime, 108) AS SetupEndTime,

    SDM.TotalSetupTime,
STM.IdealTime AS SetupIdIdealTime,
    SPM.TotalPauseTime,
    SDLY.TotalDelayedTime,
    MTM.MachiningId,
    MTM.OperatorId AS Machining_OperatorId,
    MTM.TotalQty AS Master_TotalQty,
    MTM.ProcessedQty AS Master_ProcessedQty,
    CONVERT(VARCHAR(8), MDM.MachiningStartTime, 108) AS MachiningStartTime,
CONVERT(VARCHAR(8), MDM.MachiningEndTime, 108) AS MachiningEndTime,
    MDM.TotalMachiningTime,
MTM.IdealTime AS MachiningIdealTime,

    MPM.TotalPauseTime AS Machining_PauseTime,
    MDel.TotalDelayedTime AS Machining_DelayedTime,
    MDel.ReasonCode AS Machining_ReasonCode,
    MDel.ProcessQtyDelayTime,
    QBD.ProcessedQty AS Bifurcated_ProcessedQty,
    QBD.ProcessedQtyTime,
    QBD.QtyStatus,
   CONVERT(VARCHAR(10), SDM.SetupEndTime, 23) AS SetupEndDate,
CONVERT(VARCHAR(10), MDM.MachiningEndTime, 23) AS MachiningEndDate

FROM [RouteCardProcess].[dbo].[SetUp_Trans_Master] STM
LEFT JOIN [RouteCardProcess].[dbo].[SetUp_Trans_Details_Master] SDM ON STM.SetUpID = SDM.SetUpID
LEFT JOIN [RouteCardProcess].[dbo].[SetUp_Trans_Pause_Master] SPM ON STM.SetUpID = SPM.SetUpID
LEFT JOIN [RouteCardProcess].[dbo].[Setup_Delay_Master] SDLY ON STM.SetUpID = SDLY.SetUpID
INNER JOIN [RouteCardProcess].[dbo].[Machining_Trans_Master] MTM
    ON STM.WorkOrderNo = MTM.WorkOrderNo
    AND STM.WorkCenterNo = MTM.WorkCenterNo
    AND STM.OperationNo = MTM.OperationNo
LEFT JOIN [RouteCardProcess].[dbo].[Machining_Details_Master] MDM ON MTM.MachiningId = MDM.MachiningId
LEFT JOIN [RouteCardProcess].[dbo].[Machining_Pause_Master] MPM ON MTM.MachiningId = MPM.MachiningId
LEFT JOIN [RouteCardProcess].[dbo].[Machining_Delay_Master] MDel ON MTM.MachiningId = MDel.MachiningId
LEFT JOIN [RouteCardProcess].[dbo].[Qty_Bifurcation_Details] QBD ON MTM.MachiningId = QBD.MachiningId
WHERE STM.WorkOrderNo = @WorkOrderNo

UNION ALL

-- Case 2: Setup data only, no matching Machining on all 3 keys
SELECT 
    STM.WorkOrderNo,
    STM.SetUpID,
    STM.OperatorId AS Master_OperatorId,
    STM.WorkCenterNo,
    STM.OperationNo,
   CONVERT(VARCHAR(8), SDM.SetupStartTime, 108) AS SetupStartTime,
CONVERT(VARCHAR(8), SDM.SetupEndTime, 108) AS SetupEndTime,

    SDM.TotalSetupTime,
STM.IdealTime AS SetupIdIdealTime,
    SPM.TotalPauseTime,
    SDLY.TotalDelayedTime,
    NULL AS MachiningId,
    NULL AS Machining_OperatorId,
    NULL AS Master_TotalQty,
    NULL AS Master_ProcessedQty,
    NULL AS MachiningStartTime,
    NULL AS MachiningEndTime,
    NULL AS TotalMachiningTime,
NULL AS MachiningIdealTime,
    NULL AS Machining_PauseTime,
    NULL AS Machining_DelayedTime,
    NULL AS Machining_ReasonCode,
    NULL AS ProcessQtyDelayTime,
    NULL AS Bifurcated_ProcessedQty,
    NULL AS ProcessedQtyTime,
    NULL AS QtyStatus,
    CONVERT(VARCHAR(10), SDM.SetupEndTime, 23) AS SetupEndDate,  
    NULL AS MachiningEndDate
FROM [RouteCardProcess].[dbo].[SetUp_Trans_Master] STM
LEFT JOIN [RouteCardProcess].[dbo].[SetUp_Trans_Details_Master] SDM ON STM.SetUpID = SDM.SetUpID
LEFT JOIN [RouteCardProcess].[dbo].[SetUp_Trans_Pause_Master] SPM ON STM.SetUpID = SPM.SetUpID
LEFT JOIN [RouteCardProcess].[dbo].[Setup_Delay_Master] SDLY ON STM.SetUpID = SDLY.SetUpID
WHERE STM.WorkOrderNo = @WorkOrderNo
  AND NOT EXISTS (
      SELECT 1 
      FROM [RouteCardProcess].[dbo].[Machining_Trans_Master] MTM
      WHERE STM.WorkOrderNo = MTM.WorkOrderNo
        AND STM.WorkCenterNo = MTM.WorkCenterNo
        AND STM.OperationNo = MTM.OperationNo
  )

UNION ALL

-- Case 3: Machining data only, no matching Setup on all 3 keys
SELECT 
    MTM.WorkOrderNo,
    NULL AS SetUpID,
    NULL AS Master_OperatorId,
    MTM.WorkCenterNo,
    MTM.OperationNo,
    NULL AS SetupStartTime,
    NULL AS SetupEndTime,
    NULL AS TotalSetupTime,
NULL AS SetupIdIdealTime,
    NULL AS TotalPauseTime,
    NULL AS TotalDelayedTime,
    MTM.MachiningId,
    MTM.OperatorId AS Machining_OperatorId,
    MTM.TotalQty AS Master_TotalQty,
    MTM.ProcessedQty AS Master_ProcessedQty,
    CONVERT(VARCHAR(8), MDM.MachiningStartTime, 108) AS MachiningStartTime,
CONVERT(VARCHAR(8), MDM.MachiningEndTime, 108) AS MachiningEndTime,
    MDM.TotalMachiningTime,
MTM.IdealTime AS MachiningIdealTime,
    MPM.TotalPauseTime AS Machining_PauseTime,
    MDel.TotalDelayedTime AS Machining_DelayedTime,
    MDel.ReasonCode AS Machining_ReasonCode,
    MDel.ProcessQtyDelayTime,
    QBD.ProcessedQty AS Bifurcated_ProcessedQty,
    QBD.ProcessedQtyTime,
    QBD.QtyStatus,
    NULL AS SetupEndDate,
  CONVERT(VARCHAR(10), MDM.MachiningEndTime, 23) AS MachiningEndDate
FROM [RouteCardProcess].[dbo].[Machining_Trans_Master] MTM
LEFT JOIN [RouteCardProcess].[dbo].[Machining_Details_Master] MDM ON MTM.MachiningId = MDM.MachiningId
LEFT JOIN [RouteCardProcess].[dbo].[Machining_Pause_Master] MPM ON MTM.MachiningId = MPM.MachiningId
LEFT JOIN [RouteCardProcess].[dbo].[Machining_Delay_Master] MDel ON MTM.MachiningId = MDel.MachiningId
LEFT JOIN [RouteCardProcess].[dbo].[Qty_Bifurcation_Details] QBD ON MTM.MachiningId = QBD.MachiningId
WHERE MTM.WorkOrderNo = @WorkOrderNo
  AND NOT EXISTS (
      SELECT 1 
      FROM [RouteCardProcess].[dbo].[SetUp_Trans_Master] STM
      WHERE MTM.WorkOrderNo = STM.WorkOrderNo
        AND MTM.WorkCenterNo = STM.WorkCenterNo
        AND MTM.OperationNo = STM.OperationNo
  );
";

            using var connection = CreateConnection();
            var result = (await connection.QueryAsync<RouteCardReportModel>(sql, new { WorkOrderNo = workOrderNo })).ToList();
            foreach (var item in result)
            {
                DateTime shiftTime;

                if (!string.IsNullOrWhiteSpace(item.MachiningEndTime) &&
                    DateTime.TryParse(item.MachiningEndTime, out shiftTime))
                {
                    item.Shift = _logInRepository.GetCurrentShift(shiftTime);
                }
                else
                {
                    item.Shift = _logInRepository.GetCurrentShift(DateTime.Now);
                }
            }
            return result;
        }
    }
}
