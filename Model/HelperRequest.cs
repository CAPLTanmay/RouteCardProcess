namespace RouteCardProcess.Model
{
    public class HelperRequest
    {
        public string OperatorId { get; set; }
        public string Password { get; set; }
        public string SetupId { get; set; }
        public string MachiningId { get; set; }
    }

    public class EndHelperRequest
    {
        public string OperatorId { get; set; }
        public string SetupId { get; set; }
        public string MachiningId { get; set; }
    }
}
