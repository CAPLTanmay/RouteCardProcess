namespace RouteCardProcess.Model.Entities
{
     public class OperatorHelperLog
    {
        public int SrNo { get; set; }
        public string OperatorId { get; set; }
        public string? SetupId { get; set; }
        public string? MachiningId { get; set; }
        public DateTime OperatorStartTime { get; set; }
        public string MainOperatorId { get; set; }
        public bool IsRelease { get; set; }
        public string? MSTIdleCode { get; set; }
    }
}
