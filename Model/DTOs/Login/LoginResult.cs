using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Model.DTOs.Login
{
    public class LoginResult
    {
        public bool IsSuccess { get; set; }
        public bool IsTempPassword { get; set; }
        public string FailureReason { get; set; }
        public LoginUserDto? User { get; set; }
    }
}
