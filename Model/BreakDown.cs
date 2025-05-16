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
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

}
