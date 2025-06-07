public class RouteCardReportModel
{
    public string WorkOrderNo { get; set; }
    public string SetUpID { get; set; }
    public string Master_OperatorId { get; set; }
    public string? SetupStartTime { get; set; }
    public string? SetupEndTime { get; set; }
    public TimeSpan? TotalSetupTime { get; set; }
    public TimeSpan? SetupIdIdealTime { get; set; }
    public TimeSpan? TotalPauseTime { get; set; }
    public TimeSpan? TotalDelayedTime { get; set; }
    public string MachiningId { get; set; }
    public string Machining_OperatorId { get; set; }
    public string WorkCenterNo { get; set; }
    public string OperationNo { get; set; }
    public decimal? Master_TotalQty { get; set; }
    public decimal? Master_ProcessedQty { get; set; }
    public string? MachiningStartTime { get; set; }
    public string? MachiningEndTime { get; set; }
    public string? TotalMachiningTime { get; set; }
    public TimeSpan? MachiningIdealTime { get; set; }
    public string? Machining_PauseTime { get; set; }
    public TimeSpan? Machining_DelayedTime { get; set; }
    public string Machining_ReasonCode { get; set; }
    public TimeSpan? ProcessQtyDelayTime { get; set; }
    public decimal? Bifurcated_ProcessedQty { get; set; }
    public TimeSpan? ProcessedQtyTime { get; set; }
    public string QtyStatus { get; set; }
    public string? SetupEndDate { get; set; }
    public string? MachiningEndDate { get; set; }
    public string Shift { get; set; }

}
