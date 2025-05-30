using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Login
{
    public class LoginRequest
    {
        [Required]
        public string OperatorId { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class LogoutRequest
    {
        [Required]
        public string WorkCenterNo { get; set; }

        [Required]
        public string WorkOrderNo { get; set; }

        [Required]
        public string OperationNo { get; set; }
    }
}
