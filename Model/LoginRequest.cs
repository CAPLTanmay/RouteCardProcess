namespace RouteCardProcess.Model
{
    public class LoginRequest
    {
        public string OperatorId { get; set; }
        public string Password { get; set; }
    }

    public class LogoutRequest
    {
        public string WorkCenterNo { get; set; }
        public string WorkOrderNo { get; set; }
        public string OperationNo { get; set; }
    }

}
