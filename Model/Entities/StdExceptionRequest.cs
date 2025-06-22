namespace RouteCardProcess.Model.Entities
{
    public class StdExceptionRequest
    {
        public string Reason_Code { get; set; }
        public string Comments_Std { get; set; }
        public bool? IsActive { get; set; }
    }

    public class DeleteStdExceptionRequest
    {
        public string Reason_Code { get; set; }

    }
}
