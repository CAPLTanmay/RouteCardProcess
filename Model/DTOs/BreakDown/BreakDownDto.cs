using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.BreakDownDto
{
    public class BreakDownStartRequest
    {
        [Required]
        public required string WorkCenterNo { get; set; }
        [Required]
        public required string OperatorId { get; set; }
        public string? BreakDownReasonCode { get; set; }  
        
    }
    public class BreakDownEndRequest
    {
        [Required]
        public required string WorkCenterNo { get; set; }
        public string? OperatorId { get; set; }
        public string? BreakDownReasonCode { get; set; } 
    }
}
