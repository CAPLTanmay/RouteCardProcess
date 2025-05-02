namespace RouteCardProcess.Model
{
    public class MachiningDto
    {
        public string? WorkCenterNo { get; set; }
        public string? WorkOrderNo { get; set; }
        public string? OperationNo { get; set; }
        public string? OperatorId { get; set; }
        public string? TotalQty { get; set; }
        public string? ProcessedQty { get; set; }
        public string IdealTime { get; set; } = string.Empty;
    }

    public class MachiningMaster
    {
        public string? MachiningId { get; set; }
        public string WorkCenterNo { get; set; } = string.Empty;
        public string WorkOrderNo { get; set; } = string.Empty;
        public string OperationNo { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public TimeSpan? IdealTime { get; set; }
        public string? MachiningStatus { get; set; }
        public DateTime OperatorStartTime { get; set; }
        public DateTime OperatorEndTime { get; set; }
        public string TotalQty { get; set; } = string.Empty;
        public string ProcessedQty { get; set; } = string.Empty;
    }

    public class MachiningIdentifierRequest
    {
        public string MachiningId { get; set; } = string.Empty;
    }

    public class MachiningPauseRequest
    {
        public string MachiningId { get; set; } = string.Empty;
        public string? PauseCode { get; set; }
    }

    public class AddQuantity
    {
        public string MachiningId { get; set; } = string.Empty;
        public string? TotalQty { get; set; }
        public List<QuantityList> QuantityList { get; set; } = new();
    }

    public class QuantityList
    {
        public string MachiningStatus { get; set; } = string.Empty;
        public string ProcessedQty { get; set; } = string.Empty;
    }

    public class MachiningDelayRequest
    {
        public string MachiningId { get; set; } = string.Empty;
        public TimeSpan? TotalDelayedTime { get; set; }
        public string? TotalQty { get; set; }
        public List<MachiningDelayReasonCode> Delays { get; set; } = new();
    }

    public class MachiningDelayReasonCode
    {
        public string ProcessedQty { get; set; } = string.Empty;
        public TimeSpan DelayTime { get; set; }
        public string DelayReasonCode { get; set; } = string.Empty;
    }
}
