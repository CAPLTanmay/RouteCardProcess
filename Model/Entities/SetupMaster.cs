namespace RouteCardProcess.Model.Entities
{
    public class SetupMaster
    {
        public string? SetUpID { get; set; }
        public string WorkCenterNo { get; set; }
        public string DepartmentId { get; set; }
        public string ProductionOrderNo { get; set; }
        public string OperationNo { get; set; }
        public string OperatorId { get; set; }
        public TimeSpan? StandardSetupTime { get; set; }
        public string? SetupStatus { get; set; }
        public DateTime OperatorStartTime { get; set; }
        public DateTime OperatorEndTime { get; set; }
        public DateTime? SetupStartTime { get; set; }
        public DateTime? SetupEndTime { get; set; }
        public string? ActualSetupTime { get; set; }
        public string? TotalSetupTime { get; set; }
        public string OrderTypeDesc { get; set; }
        public string? TimeDiff { get; set; }  // hh:mm:ss format

        public int? DebugElapsedSeconds { get; set; }
        public int? DebugPauseSeconds { get; set; }
        public int? DebugFinalSeconds { get; set; }
    }
}
