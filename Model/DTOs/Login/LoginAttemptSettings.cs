namespace RouteCardProcess.Model.DTOs.Login
{
    public class LoginAttemptSettings
    {
        public int MaxAttempts { get; set; }
        public int LockoutDurationInMinutes { get; set; }
    }
}
