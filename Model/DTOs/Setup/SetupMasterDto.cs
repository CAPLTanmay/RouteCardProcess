using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Setup
{
    public class SetupMasterDto
    {
        public string? SetUpID { get; set; }

        [Required]
        public string WorkCenterNo { get; set; }

        [Required]
        public string WorkOrderNo { get; set; }

        [Required]
        public string OperationNo { get; set; }

        [Required]
        public string OperatorId { get; set; }

        public string? IdealTime { get; set; }
    }
}
