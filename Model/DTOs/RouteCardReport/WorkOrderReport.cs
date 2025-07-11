using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.RouteCardReport
{
    public class WorkOrderRequest
    {
        [Required]
        public string WorkOrderNo { get; set; }
    }

    public class LossOrderRequestDto
    {
        public string? SetupId { get; set; }
        public string? MachiningId { get; set; }
    }


    public class RouteCardReportFilterRequest
    {
        public string? OperatorId { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public string? ProductionOrderNo { get; set; }
        public string? Department { get; set; }
        public string? WorkCenterNo { get; set; }
    }

    public class RouteCardReportDto
    {
        public string OperatorName { get; set; }
        public string CurrentShift { get; set; }
        public string OperatorId { get; set; }
        public string ProductionOrderNo { get; set; }
        public string WorkCenterNo { get; set; }
        public string WorkCenterText { get; set; }
        public string Material { get; set; }
        public string MaterialText { get; set; }

        public string MrpController { get; set; }
        public string ProductionScheduler { get; set; }
        public string ProcessingUnit { get; set; }
        public string ProductionUnit { get; set; }
        public string OperationNo { get; set; }
        public string OperationDescription { get; set; }
        public string OrderType { get; set; }
        public int TotalQty { get; set; }
        public int Pending_qty { get; set; }
        public int CompletedQty { get; set; }
        public string SetupId { get; set; }
        public DateTime SetupStartTime { get; set; }
        public DateTime SetupEndTime { get; set; }
        public int ActualSetupTime { get; set; }

        public int TotalSetupIdleMinutes { get; set; }
        public string TotalSetupIdle_HHMMSS { get; set; }

        public int TotalSetupExceptionsMinutes { get; set; }
        public string TotalSetupExceptions_HHMMSS { get; set; }

        public DateTime? SetupOperatorStartTime { get; set; }
        public DateTime? SetupOperatorEndTime { get; set; }
        public string MachiningId { get; set; }
        public DateTime MachiningStartTime { get; set; }
        public DateTime MachiningEndTime { get; set; }
        public int ActualMachiningTime { get; set; }
        public int TotalMachiningIdleMinutes { get; set; }
        public string TotalMachiningIdle_HHMMSS { get; set; }

        public int TotalMachiningExceptionsMinutes { get; set; }
        public string TotalMachiningExceptions_HHMMSS { get; set; }

        public DateTime? MachiningOperatorStartTime { get; set; }
        public DateTime? MachiningOperatorEndTime { get; set; }
        public int ActualOperationTime { get; set; }
        public int IdleOperationTime { get; set; }
        public DateTime? FinishDate {get;set;}
        public int ActualLaborTime { get; set; }
        public decimal ActualLaborTime_Hours { get; set; }
    }

    public class LossOrderResponseDto
    {
        public string ORDER { get; set; }
        public List<SetupIdleDto> SetupIdleRecords { get; set; }
        public List<MachiningIdleDto> MachiningIdleRecords { get; set; }
    }
    public class SetupIdleDto
    {
        public string SetUpID { get; set; }
        public string OperatorId { get; set; }
        public string ORDER { get; set; }
        public string MSTIdleCode { get; set; }
        public TimeSpan SetupIdleTime { get; set; }
    }

    public class MachiningIdleDto
    {
        public string MachiningID { get; set; }
        public string OperatorId { get; set; }
        public string ORDER { get; set; }
        public string MSTIdleCode { get; set; }
        public TimeSpan MachiningIdleTime { get; set; }
    }



}
