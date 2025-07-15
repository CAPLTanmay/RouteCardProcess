using System.Text.Json.Serialization;

namespace RouteCardProcess.Model.DTOs.RouteCardReport
{
    public class FullUpdateDto
    {
        public SetupUpdateDto Setup { get; set; }
        public MachiningUpdateDto Machining { get; set; }
    }

    public class SetupUpdateDto
    {
        public string SetUpID { get; set; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? SetupStartDate { get; set; }

        public TimeSpan? SetupStartTime { get; set; }

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? SetupEndDate { get; set; }

        public TimeSpan? SetupEndTime { get; set; }
        public int UpdatedOperatorId { get; set; }

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
        public DateTime? MachiningStartDate { get; set; }

        public TimeSpan? MachiningStartTime { get; set; }

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? MachiningEndDate { get; set; }

        public TimeSpan? MachiningEndTime { get; set; }
        public int UpdatedOperatorId { get; set; }

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
        public int OperatorId { get; set; }
        public int NewCompletedQty { get; set; }
    }

}
