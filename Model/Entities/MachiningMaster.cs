namespace RouteCardProcess.Model.Entities
{
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
        public DateTime? MachiningStartTime { get; set; }
        public DateTime? MachiningEndTime { get; set; }
        public string? TotalMachiningTime { get; set; }
        public string ActualMachiningTime { get; set; }
    }
}
