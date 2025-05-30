using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Machining
{
        public class MachiningDto
        {
        public string? MachiningId { get; set; }

        [Required]
        public string? WorkCenterNo { get; set; }

        [Required]
        public string? WorkOrderNo { get; set; }

        [Required]
        public string? OperationNo { get; set; }

        [Required]
        public string? OperatorId { get; set; }

        [Required]
        public string? TotalQty { get; set; }

        public string? ProcessedQty { get; set; }

        public string IdealTime { get; set; } = string.Empty;
    }
}
