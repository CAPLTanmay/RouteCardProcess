using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Machining
{
        public class MachiningDto
        {
        public string? MachiningId { get; set; }
        public string? WorkCenterNo { get; set; }
        public string? DepartmentId { get; set; }
        public string? ProductionOrderNo { get; set; }
        public string? OperationNo { get; set; }
        public string? OperatorId { get; set; }
        public string StandardMachiningTime { get; set; } = string.Empty;
    }
}
