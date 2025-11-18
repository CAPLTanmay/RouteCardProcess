using System.ComponentModel.DataAnnotations;
using RouteCardProcess.Middleware;

namespace RouteCardProcess.Model.DTOs.Machining
{
    // ------------------------------------------
    //  Machining Delay Request (Main Wrapper)
    // ------------------------------------------
    public class MachiningDelayRequest
    {
        [Required(ErrorMessage = "MachiningId is required.")]
        [SafeText(SafeTextPattern.AlphaNumeric, 20)]
        public string MachiningId { get; set; }

        [Required(ErrorMessage = "Exceptions list is required.")]
        public List<MachiningExceptionsRequest> Exceptions { get; set; } = new();

        [Required(ErrorMessage = "IdleTimes list is required.")]
        public List<MachiningIdleTimeRequest> IdleTimes { get; set; } = new();
    }

    // ------------------------------------------
    //  Machining Exception Request
    // ------------------------------------------
    public class MachiningExceptionsRequest
    {
        [Required(ErrorMessage = "ExceptionsReasonCode is required.")]
        [SafeText(SafeTextPattern.AlphaNumeric, 10)]
        public string ExceptionsReasonCode { get; set; }

        //[SafeText(SafeTextPattern.AlphaNumeric, 10, AllowEmpty = true)]
        public string? Std_exceptions_ReasonCode { get; set; }

        //[SafeText(SafeTextPattern.AlphaNumericWithSymbols, 250,AllowEmpty = true)]
        public string? Std_exceptions_Remark { get; set; }

        public TimeSpan? ExceptionsTime { get; set; } // stored as hh:mm:ss
    }

    // ------------------------------------------
    //  Machining Idle Time Request
    // ------------------------------------------
    public class MachiningIdleTimeRequest
    {
        [Required(ErrorMessage = "MSTIdleCode is required.")]
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public string MSTIdleCode { get; set; }

        public TimeSpan? MachiningIdleTime { get; set; } // stored as hh:mm:ss
    }
}
