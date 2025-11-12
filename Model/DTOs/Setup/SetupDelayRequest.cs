using System.ComponentModel.DataAnnotations;
using RouteCardProcess.Middleware;

namespace RouteCardProcess.Model.DTOs.Setup
{
    public class SetupDelayRequest
    {
        [SafeText(SafeTextPattern.AlphaNumeric, 20)]
        public string SetUpID { get; set; }
        public string SetUpStatus { get; set; }
        public List<ExceptionsRequest> Exceptions { get; set; } 
        public List<IdleTimeRequest> IdleTimes { get; set; }
    }
    public class ExceptionsRequest
    {
        [SafeText(SafeTextPattern.AlphaNumeric, 10)]
        public string ExceptionsReasonCode { get; set; }
        [SafeText(SafeTextPattern.AlphaNumeric, 10, AllowEmpty = true)]
        public string Std_exceptions_ReasonCode { get; set; }
        [SafeText(SafeTextPattern.AlphaNumericWithSymbols, 250, AllowEmpty = true)]
        public string Std_exceptions_Remark { get; set; }
        public TimeSpan? ExceptionsTime { get; set; }
    }

    public class IdleTimeRequest
    {
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public string MSTIdleCode { get; set; }
        public TimeSpan? SetupIdleTime { get; set; }
    }

}
