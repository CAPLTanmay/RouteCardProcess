namespace RouteCardProcess.Model.DTOs.Manualdata
{
    public class ManualDataUpdateResult
    {
        public bool Success { get; set; }
        public string? SetupId { get; set; }
        public string? MachiningId { get; set; }
        public int OperatorId { get; set; }
        public string? Message { get; set; }
    }
}

