namespace RouteCardProcess.Model.Entities
{
    public class ExceptionReasonRequest
    {
        public string Reason_Code { get; set; }
        public string Reason_desc { get; set; }
        public string Reason_descM { get; set; }
        public string? Comments_Std { get; set; }
        public string? FullReasonDescription { get; set; }
        public bool? IsActive { get; set; }
    }

    public class DeleteExceptionRequest
    {
        public string Reason_Code { get; set; }
    }
}
