namespace RouteCardProcess.Model.DTOs.Setup
{
    public class EndSetupResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? StandardSetupTime { get; set; }
        public string TimeDiff { get; set; }  // hh:mm:ss format
    }
}
