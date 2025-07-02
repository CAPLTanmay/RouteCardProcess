namespace RouteCardProcess.Model.Entities
{
    public class LossOrderRequest
    {
        public string LossOrderDepartment { get; set; }
        public int LossOrderYearMonth { get; set; }
        public long LossOrderNumber { get; set; }
        public string LossOrderDesc { get; set; }
        public bool? IsActive { get; set; }
    }

    public class DeleteLossOrderRequest
    {
        public string LossOrderDepartment { get; set; }
        public int? LossOrderYearMonth { get; set; }
        public long LossOrderNumber { get; set; }
    }
}
