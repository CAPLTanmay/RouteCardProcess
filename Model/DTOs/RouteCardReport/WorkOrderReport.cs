using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.RouteCardReport
{
    public class WorkOrderRequest
    {
        [Required]
        public string WorkOrderNo { get; set; }
    }
}
