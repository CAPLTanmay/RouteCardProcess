namespace RouteCardProcess.Model.DTOs.Machining
{
    public class EndMachiningResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? StandardMachiningTime { get; set; }  // hh:mm:ss format
        public string? MachiningTimeDiff { get; set; }      // hh:mm:ss format
    }
}
