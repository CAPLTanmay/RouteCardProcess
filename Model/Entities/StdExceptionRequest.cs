using RouteCardProcess.Middleware;

namespace RouteCardProcess.Model.Entities
{
    public class StdExceptionRequest
    {
        [SafeText(SafeTextPattern.AlphaNumeric, 10)]
        public string Reason_Code { get; set; }
        [SafeText(SafeTextPattern.AlphaNumericWithSymbols, 250)]
        public string Comments_Std { get; set; }
        public bool? IsActive { get; set; }
    }
    public class DeleteStdExceptionRequest
    {
        public string Reason_Code { get; set; }

    }
}
