using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.BreakDownDto
{
    public class BreakDownStartRequest
    {
        [Required]
        public required string WorkCenterNo { get; set; }

        [Required]
        public required string OperatorId { get; set; }

        public string? BreakdownCodeGroup { get; set; }

        public string? BreakdownCode { get; set; }
    }

    public class BreakDownEndRequest
    {
        [Required]
        public required string WorkCenterNo { get; set; }
        public string? OperatorId { get; set; }
        public string? BreakDownReasonCode { get; set; }
    }

    public class BreakDownResponse
    {
        public bool IsDbSuccess { get; set; }
        public bool IsMailSent { get; set; }
        public bool IsSapPosted { get; set; }
        public string Message { get; set; } = string.Empty;
    }

  
}
