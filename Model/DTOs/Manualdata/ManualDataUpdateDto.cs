namespace RouteCardProcess.Model.DTOs.Manualdata
{
    public class ManualDataUpdateDto
    {
        public string WorkOrder { get; set; }
        public string WorkCenter { get; set; }
        public string OperationNo { get; set; }

        public int OperatorId { get; set; }
        public int? L_CompletedQty { get; set; }

        public DateTime? SetupStartTime { get; set; }
        public DateTime? SetupEndTime { get; set; }
        public DateTime? MachiningStartTime { get; set; }
        public DateTime? MachiningEndTime { get; set; }
    }
}
