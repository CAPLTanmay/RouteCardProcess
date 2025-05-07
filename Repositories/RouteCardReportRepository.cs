using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Model;

namespace RouteCardProcess.Repositories
{
    public class RouteCardReportRepository
    {
        private readonly IConfiguration _config;

        public RouteCardReportRepository(IConfiguration config)
        {
            _config = config;
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
            SDM.SetupStartTime,
            SDM.SetupEndTime,
            SDM.TotalSetupTime,
            SPM.TotalPauseTime,
            SDLY.TotalDelayedTime,
            MTM.MachiningId,
            MTM.OperatorId AS Machining_OperatorId,
            MTM.WorkCenterNo,
            MTM.OperationNo,
            MTM.TotalQty AS Master_TotalQty,
            MTM.ProcessedQty AS Master_ProcessedQty,
            MDM.MachiningStartTime,
            MDM.MachiningEndTime,
            MDM.TotalMachiningTime,
            MPM.TotalPauseTime AS Machining_PauseTime,
            MDel.TotalDelayedTime AS Machining_DelayedTime,
            MDel.ReasonCode AS Machining_ReasonCode,
            MDel.ProcessQtyDelayTime,
            QBD.ProcessedQty AS Bifurcated_ProcessedQty,
            QBD.ProcessedQtyTime,
            QBD.QtyStatus
        FROM [RouteCardProcess].[dbo].[SetUp_Trans_Master] STM
        LEFT JOIN [RouteCardProcess].[dbo].[SetUp_Trans_Details_Master] SDM ON STM.SetUpID = SDM.SetUpID
        LEFT JOIN [RouteCardProcess].[dbo].[SetUp_Trans_Pause_Master] SPM ON STM.SetUpID = SPM.SetUpID
        LEFT JOIN [RouteCardProcess].[dbo].[Setup_Delay_Master] SDLY ON STM.SetUpID = SDLY.SetUpID
        LEFT JOIN [RouteCardProcess].[dbo].[Machining_Trans_Master] MTM
            ON STM.WorkOrderNo = MTM.WorkOrderNo
            AND STM.WorkCenterNo = MTM.WorkCenterNo
            AND STM.OperationNo = MTM.OperationNo
        LEFT JOIN [RouteCardProcess].[dbo].[Machining_Details_Master] MDM ON MTM.MachiningId = MDM.MachiningId
        LEFT JOIN [RouteCardProcess].[dbo].[Machining_Pause_Master] MPM ON MTM.MachiningId = MPM.MachiningId
        LEFT JOIN [RouteCardProcess].[dbo].[Machining_Delay_Master] MDel ON MTM.MachiningId = MDel.MachiningId
        LEFT JOIN [RouteCardProcess].[dbo].[Qty_Bifurcation_Details] QBD ON MTM.MachiningId = QBD.MachiningId
        WHERE STM.WorkOrderNo = @WorkOrderNo";

            using var connection = CreateConnection();
            return await connection.QueryAsync<RouteCardReportModel>(sql, new { WorkOrderNo = workOrderNo });
        }

    }
}
