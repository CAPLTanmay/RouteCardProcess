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
        public string NOTIF_NUM { get; set; } 
        public string? STATUS { get; set; } 
    }

    public class BreakDownResponse
    {
        public bool IsDbSuccess { get; set; }
        public bool IsMailSent { get; set; }
        public bool IsSapPosted { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class BreakDownRecordDto
    {
        public string WorkCenterNo { get; set; }
        public string EquipmentNo { get; set; }
        public string OperatorId { get; set; }
        public string BreakdownCodeGroup { get; set; }
        public string BreakdownCode { get; set; }
        public string BreakNotificationNo { get; set; }
        public string BreakdownNotificationStatus { get; set; }
        public DateTime? BreakdownStartTime { get; set; }
        public DateTime? BreakdownEndTime { get; set; }
        public TimeSpan? TotalBreakdownTime { get; set; }
        public string BreakdownCategory { get; set; }
    }

}
