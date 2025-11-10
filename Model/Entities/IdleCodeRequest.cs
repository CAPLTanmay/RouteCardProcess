using RouteCardProcess.Middleware;

namespace RouteCardProcess.Model.Entities
{
    public class IdleCodeRequest
    {
        [SafeText(SafeTextPattern.NumericOnly, 4)]
        public string Plant { get; set; }

        [SafeText(SafeTextPattern.NumericOnly, 4)]
        public string IdleCode { get; set; }

        [SafeText(SafeTextPattern.AlphaNumericWithSymbols, 100)]
        public string IdleCodeDesc { get; set; }

        [SafeText(SafeTextPattern.UnicodeText, 100)] 
        public string IdleCodeDescM { get; set; }

        [SafeText(SafeTextPattern.AlphaNumericWithSymbols, 200)]
        public string? FullIdleDescription { get; set; }

        public bool? IsActive { get; set; }
    }

    public class DeleteCodeRequest
    {
        public string Plant { get; set; }
        public string IdleCode { get; set; }
    }
}
