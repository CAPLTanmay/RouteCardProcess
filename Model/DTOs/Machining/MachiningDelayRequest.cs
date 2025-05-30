using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Machining
{
    public class MachiningDelayRequest
    {
        [Required]
        public string MachiningId { get; set; } = string.Empty;

        public TimeSpan? TotalDelayedTime { get; set; }

        public string? TotalQty { get; set; }

        public List<MachiningDelayReasonCode>? Delays { get; set; } = new();
    }

    public class MachiningDelayReasonCode
    {
        public string ProcessedQty { get; set; } = string.Empty;
        public TimeSpan DelayTime { get; set; }
        public string DelayReasonCode { get; set; } = string.Empty;
    }
}
