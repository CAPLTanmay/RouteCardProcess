namespace RouteCardProcess.Model.DTOs.Setup
{
    public class EndSetupResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? StandardSetupTime { get; set; }
        public string? SetupStartTime { get; set; }
        public string? SetupEndTime { get; set; }
        public string TimeDiff { get; set; }  // hh:mm:ss format
    }
}
