using RouteCardProcess.Middleware;

namespace RouteCardProcess.Model.Entities
{
    public class ExceptionReasonRequest
    {
        [SafeText(SafeTextPattern.AlphaNumeric, 10)]
        public string Reason_Code { get; set; }

        [SafeText(SafeTextPattern.AlphaNumericWithSymbols, 200)]
        public string Reason_desc { get; set; }

        [SafeText(SafeTextPattern.UnicodeText, 200)] 
        public string Reason_descM { get; set; }

        [SafeText(SafeTextPattern.AlphaNumericWithSymbols, 250)]
        public string? Comments_Std { get; set; }

        [SafeText(SafeTextPattern.AlphaNumericWithSymbols, 250)]
        public string? FullReasonDescription { get; set; }

        public bool? IsActive { get; set; }
    }

    public class DeleteExceptionRequest
    {
        public string Reason_Code { get; set; }
    }
}
