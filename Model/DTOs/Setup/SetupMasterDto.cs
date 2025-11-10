using System.ComponentModel.DataAnnotations;
using RouteCardProcess.Middleware;

namespace RouteCardProcess.Model.DTOs.Setup
{
    public class SetupMasterDto
    {
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public string? SetUpID { get; set; }

        [Required(ErrorMessage = "WorkCenterNo is required.")]
        [SafeText(SafeTextPattern.AlphaNumeric, 20)]
        public string WorkCenterNo { get; set; }

        [SafeText(SafeTextPattern.AlphaNumeric, 10)]
        public string DepartmentId { get; set; }

        [Required(ErrorMessage = "ProductionOrderNo is required.")]
        [SafeText(SafeTextPattern.AlphaNumeric, 20)]
        public string ProductionOrderNo { get; set; }

        [Required(ErrorMessage = "OperationNo is required.")]
        [SafeText(SafeTextPattern.AlphaNumeric, 10)]
        public string OperationNo { get; set; }

        [Required(ErrorMessage = "OperatorId is required.")]
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public string OperatorId { get; set; }

        public string? StandardSetupTime { get; set; } // Format: "hh:mm:ss"
    }
}
