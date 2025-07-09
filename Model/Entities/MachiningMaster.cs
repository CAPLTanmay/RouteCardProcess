namespace RouteCardProcess.Model.Entities
{
    public class MachiningMaster
    {
        public string? MachiningId { get; set; }
        public string WorkCenterNo { get; set; } = string.Empty;
        public string? DepartmentId { get; set; }
        public string ProductionOrderNo { get; set; } = string.Empty;
        public string OperationNo { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public TimeSpan? StandardMachiningTime { get; set; }
        public string? MachiningStatus { get; set; }
        public DateTime OperatorStartTime { get; set; }
        public DateTime OperatorEndTime { get; set; }
        public DateTime? MachiningStartTime { get; set; }
        public DateTime? MachiningEndTime { get; set; }
        public string? TotalMachiningTime { get; set; }
        public string ActualMachiningTime { get; set; }
        public int TotalQty { get; set; }
        public string OrderTypeDesc { get; set; }
    }

    public class SapRoutingInfo
    {
        public int TotalQty { get; set; }
        public string OrderTypeDesc { get; set; }
    }

}
