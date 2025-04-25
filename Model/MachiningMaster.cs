namespace RouteCardProcess.Model
{
    public class MachiningDto
    {
        public string? WorkCenterNo { get; set; }
        public string? WorkOrderNo { get; set; }
        public string? OperationNo { get; set; }
        public string? OperatorId { get; set; }
        public string? TotalQty { get; set; }
        public string? ProcessedQty {  get; set; }
        public string IdealTime { get; set; } // IdealTime in minutes (converted to TimeSpan)
    }

    public class MachiningMaster
    {
        public string? MachiningId { get; set; }
        public string WorkCenterNo { get; set; }
        public string WorkOrderNo { get; set; }
        public string OperationNo { get; set; }
        public string OperatorId { get; set; }
        public TimeSpan? IdealTime { get; set; }
        public string? MachiningStatus { get; set; }
        public DateTime OperatorStartTime { get; set; }
        public DateTime OperatorEndTime { get; set; }
        public string TotalQty { get; set; }
        public string ProcessedQty { get; set; }
    }

    public class MachiningIdentifierRequest
    {
        public string MachiningId { get; set; }
    }

    public class MachiningPauseRequest
    {
        public string MachiningId { get; set; }
        public string? PauseCode { get; set; }
    }

    public class MachiningDelayRequest
    {
        public string MachiningId { get; set; }
        public string MachiningStatus { get; set; }
        public List<DelayRequest> Delays { get; set; }
    }

    public class MachiningDelayReasonCode
    {
        public string DelayReasonCode { get; set; }
        public TimeSpan DelayTime { get; set; }
    }
}
