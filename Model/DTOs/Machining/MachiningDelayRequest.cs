using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Machining
{
    public class MachiningDelayRequest
    {
        public string MachiningId { get; set; }
        public List<MachiningExceptionsRequest> Exceptions { get; set; }
        public List<MachiningIdleTimeRequest> IdleTimes { get; set; }
    }
    public class MachiningExceptionsRequest
    {
        public string ExceptionsReasonCode { get; set; }
        public string Std_exceptions_ReasonCode { get; set; }
        public string Std_exceptions_Remark { get; set; }
        public TimeSpan? ExceptionsTime { get; set; }
    }

    public class MachiningIdleTimeRequest
    {
        public string MSTIdleCode { get; set; }
        public TimeSpan? SetupIdleTime { get; set; }
    }
}

