namespace RouteCardProcess.Model.DTOs.RBACEmployee
{
    public class ResetPasswordDto
    {
        public int OperatorId { get; set; }          
        public string TempPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
