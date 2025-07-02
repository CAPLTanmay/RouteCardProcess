namespace RouteCardProcess.Model.Entities
{
    public class BreakdownGroupCodeRequest
    {
        public string BreakdownCodeGroup { get; set; }
        public string GroupDescription { get; set; }
        public bool? IsActive { get; set; }
        public string? GroupDisplayText { get; set; }
    }
    public class BreakdownCodesByGroup
    {
        public string BreakdownCodeGroup { get; set; }
    }

}
