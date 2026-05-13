namespace RouteCardProcess.Model.DTOs.RBACEmployee
{
    public class ResetPasswordDto
    {
        public string OperatorId { get; set; }          
        public string TempPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
