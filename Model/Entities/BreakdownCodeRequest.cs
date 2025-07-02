namespace RouteCardProcess.Model.Entities
{
    public class BreakdownCodeRequest
    {
        public string BreakdownCodeGroup { get; set; }
        public string BreakdownCode { get; set; }
        public string BreakdownDescription { get; set; }
        public bool? IsActive { get; set; }
        public string? BreakdownDisplayText { get; set; }
    }
}
