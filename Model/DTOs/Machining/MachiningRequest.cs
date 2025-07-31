namespace RouteCardProcess.Model.DTOs.Machining
{
    public class MachiningOperatorStartRequest
    {
        public string MachiningId { get; set; }
        public string OperatorId { get; set; }
        public DateTime OperatorStartTime { get; set; }
    }


    public class AddQuantityRequest
    {
        public string MachiningId { get; set; }
        public int TotalQty { get; set; }
        public int ProcessedQty { get; set; }
        public string QtyStatus { get; set; }
    }

    public class CompositeKeyRequest
    {
        public string WorkCenterNo { get; set; }
        public string ProductionOrderNo { get; set; } 
        public string OperationNo { get; set; }
    }


}
