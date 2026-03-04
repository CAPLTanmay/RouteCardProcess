namespace RouteCardProcess.Model.DTOs.Login
{
    public class RateLimiterPolicySettings
    {
        public int PermitLimit { get; set; }
        public int WindowInMinutes { get; set; }
        public int QueueLimit { get; set; }
    }

    public class RateLimiterSettings
    {
        public RateLimiterPolicySettings LoginRateLimit { get; set; }
        public RateLimiterPolicySettings GeneralRateLimit { get; set; }
    }

}
