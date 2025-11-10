using System.ComponentModel.DataAnnotations;
using RouteCardProcess.Middleware;

namespace RouteCardProcess.Model.DTOs.Machining
{
    public class MachiningDto
    {
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public string? MachiningId { get; set; }

        [Required(ErrorMessage = "WorkCenterNo is required.")]
        [SafeText(SafeTextPattern.AlphaNumeric, 20)]
        public string? WorkCenterNo { get; set; }

        [SafeText(SafeTextPattern.AlphaNumeric, 10)]
        public string? DepartmentId { get; set; }

        [Required(ErrorMessage = "ProductionOrderNo is required.")]
        [SafeText(SafeTextPattern.AlphaNumeric, 20)]
        public string? ProductionOrderNo { get; set; }

        [Required(ErrorMessage = "OperationNo is required.")]
        [SafeText(SafeTextPattern.AlphaNumeric, 10)]
        public string? OperationNo { get; set; }

        [Required(ErrorMessage = "OperatorId is required.")]
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public string? OperatorId { get; set; }

        [Required(ErrorMessage = "StandardMachiningTime is required.")]
        public string StandardMachiningTime { get; set; } = string.Empty;
    }
}
