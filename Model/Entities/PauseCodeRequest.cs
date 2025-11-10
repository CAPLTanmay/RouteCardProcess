using RouteCardProcess.Middleware;

namespace RouteCardProcess.Model.Entities
{
    public class PauseCodeRequest
    {
        [SafeText(SafeTextPattern.NumericOnly, 4)]
        public string Plant { get; set; }
        [SafeText(SafeTextPattern.NumericOnly, 4)]
        public string PauseCode { get; set; }
        [SafeText(SafeTextPattern.AlphaNumeric, 100)]
        public string PauseCodeDesc { get; set; }
        [SafeText(SafeTextPattern.UnicodeText, 100)]
        public string PauseCodeDescM { get; set; }
        [SafeText(SafeTextPattern.AlphaNumeric, 100)]
        public string? FullPauseDescription {  get; set; }
        public bool? IsActive { get; set; }
    }

    public class DeletePauseCodeRequest
    {
        public string Plant { get; set; }
        public string PauseCode { get; set; }
    }
}
