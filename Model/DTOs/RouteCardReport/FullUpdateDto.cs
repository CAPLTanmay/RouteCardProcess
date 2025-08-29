using System.Text.Json.Serialization;

namespace RouteCardProcess.Model.DTOs.RouteCardReport
{
    public class FullUpdateDto
    {
        public string? OperatorId { get; set; }
        public SetupUpdateDto Setup { get; set; }
        public MachiningUpdateDto Machining { get; set; }
    }

    public class SetupUpdateDto
    {
        public string SetUpID { get; set; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public string? OperatorTransactionId { get; set; }
        public DateTime? SetupStartDate { get; set; }
        public TimeSpan? SetupStartTime { get; set; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? SetupEndDate { get; set; }
        public TimeSpan? SetupEndTime { get; set; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? OperatorStartDate { get; set; }
        public TimeSpan? OperatorStartTime { get; set; }

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? OperatorEndDate { get; set; }
        public TimeSpan? OperatorEndTime { get; set; }
        public string UpdatedOperatorId { get; set; }
        public List<IdleTimeUpdateDto> IdleTimes { get; set; } = new();
        public List<ExceptionTimeUpdateDto> ExceptionTimes { get; set; } = new();
        // Computed properties to combine Date + Time
        [JsonIgnore]
        public DateTime? SetupStartDateTime =>
            (SetupStartDate.HasValue && SetupStartTime.HasValue)
                ? SetupStartDate.Value.Date + SetupStartTime.Value
                : null;

        [JsonIgnore]
        public DateTime? SetupEndDateTime =>
            (SetupEndDate.HasValue && SetupEndTime.HasValue)
                ? SetupEndDate.Value.Date + SetupEndTime.Value
                : null;

        [JsonIgnore]
        public DateTime? OperatorStartDateTime =>
       (OperatorStartDate.HasValue && OperatorStartTime.HasValue)
           ? OperatorStartDate.Value.Date + OperatorStartTime.Value
           : null;

        [JsonIgnore]
        public DateTime? OperatorEndDateTime =>
            (OperatorEndDate.HasValue && OperatorEndTime.HasValue)
                ? OperatorEndDate.Value.Date + OperatorEndTime.Value
                : null;
    }

    public class IdleTimeUpdateDto
    {

        public string MSTIdleCode { get; set; }
        public TimeSpan NewSetupIdleTime { get; set; }
    }

    public class ExceptionTimeUpdateDto
    {
        public string StdExceptionsReasonCode { get; set; }
        public string ExceptionsReasonCode { get; set; }
        public TimeSpan NewExceptionsTime { get; set; }
    }

    public class MachiningUpdateDto
    {
        public string MachiningId { get; set; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public string? MachiningOperatorTransactionId{get;set;}
        public DateTime? MachiningStartDate { get; set; }
        public TimeSpan? MachiningStartTime { get; set; }

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? MachiningEndDate { get; set; }
        public TimeSpan? MachiningEndTime { get; set; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? OperatorStartDate { get; set; }
        public TimeSpan? OperatorStartTime { get; set; }

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? OperatorEndDate { get; set; }
        public TimeSpan? OperatorEndTime { get; set; }
        public string UpdatedOperatorId { get; set; }

        public List<MachiningIdleTimeUpdateDto> IdleTimes { get; set; } = new();
        public List<MachiningExceptionUpdateDto> ExceptionTimes { get; set; } = new();
        public List<MachiningOperatorQtyUpdateDto> OperatorQuantities { get; set; } = new();

        // Combine Date + Time for DB use
        [JsonIgnore]
        public DateTime? MachiningStartDateTime =>
            (MachiningStartDate.HasValue && MachiningStartTime.HasValue)
                ? MachiningStartDate.Value.Date + MachiningStartTime.Value
                : null;

        [JsonIgnore]
        public DateTime? MachiningEndDateTime =>
            (MachiningEndDate.HasValue && MachiningEndTime.HasValue)
                ? MachiningEndDate.Value.Date + MachiningEndTime.Value
                : null;
    }

    public class MachiningIdleTimeUpdateDto
    {
        public string MSTIdleCode { get; set; }
        public TimeSpan NewMachiningIdleTime { get; set; }
    }

    public class MachiningExceptionUpdateDto
    {
        public TimeSpan NewExceptionsTime { get; set; }
        public string StdExceptionsReasonCode { get; set; }
        public string ExceptionsReasonCode { get; set; }
    }

    public class MachiningOperatorQtyUpdateDto
    {
        public string OperatorId { get; set; }
        public int NewCompletedQty { get; set; }
    }

}
