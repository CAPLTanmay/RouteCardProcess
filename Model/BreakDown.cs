namespace RouteCardProcess.Model
{
    public class BreakDownStartRequest
    {
        public string WorkCenterNo { get; set; }
        public string OperatorId { get; set; }
        public string? BreakDownReasonCode { get; set; }  
        
    }

    public class BreakDownEndRequest
    {
        public string WorkCenterNo { get; set; }
        public string? OperatorId { get; set; }
        public string? BreakDownReasonCode { get; set; } 
    }

}
