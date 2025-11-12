using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Setup
{
    public class SetupDelayRequest
    {
        public string SetUpID { get; set; }
        public string SetUpStatus { get; set; }
        public List<ExceptionsRequest> Exceptions { get; set; } 
        public List<IdleTimeRequest> IdleTimes { get; set; }
    }
    public class ExceptionsRequest
    {
        public string ExceptionsReasonCode { get; set; }
        public string Std_exceptions_ReasonCode { get; set; }
        public string Std_exceptions_Remark { get; set; }
        public TimeSpan? ExceptionsTime { get; set; }
    }

    public class IdleTimeRequest
    {
        public string MSTIdleCode { get; set; }
        public TimeSpan? SetupIdleTime { get; set; }
    }

}
