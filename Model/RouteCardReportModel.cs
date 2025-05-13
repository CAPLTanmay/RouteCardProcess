public class RouteCardReportModel
{
    public string WorkOrderNo { get; set; }
    public string SetUpID { get; set; }
    public string Master_OperatorId { get; set; }
    public DateTime? SetupStartTime { get; set; }
    public DateTime? SetupEndTime { get; set; }
    public TimeSpan? TotalSetupTime { get; set; }
    public TimeSpan? TotalPauseTime { get; set; }
    public TimeSpan? TotalDelayedTime { get; set; }
    public string MachiningId { get; set; }
    public string Machining_OperatorId { get; set; }
    public string WorkCenterNo { get; set; }
    public string OperationNo { get; set; }
    public decimal? Master_TotalQty { get; set; } 
    public decimal? Master_ProcessedQty { get; set; }
    public DateTime? MachiningStartTime { get; set; }
    public DateTime? MachiningEndTime { get; set; }
    public string? TotalMachiningTime { get; set; }
    public TimeSpan? Machining_PauseTime { get; set; }
    public TimeSpan? Machining_DelayedTime { get; set; }
    public string Machining_ReasonCode { get; set; }
    public TimeSpan? ProcessQtyDelayTime { get; set; }
    public decimal? Bifurcated_ProcessedQty { get; set; }
    public TimeSpan? ProcessedQtyTime { get; set; }
    public string QtyStatus { get; set; }
    public string? SetupEndDate { get; set; }  
    public string? MachiningEndDate { get; set; }
}

public class WorkOrderRequest
{
    public string WorkOrderNo { get; set; }
}
