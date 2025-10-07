using RouteCardProcess.Middleware;

namespace RouteCardProcess.Model.Entities
{
    public class LossOrderRequest
    {
        [SafeText(SafeTextPattern.AlphaOnly, 50)]
        public string LossOrderDepartment { get; set; }
        [SafeText(SafeTextPattern.NumericOnly, 6)]
        public int LossOrderYearMonth { get; set; }
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public long LossOrderNumber { get; set; }

        [SafeText(SafeTextPattern.AlphaNumeric, 100)]
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
