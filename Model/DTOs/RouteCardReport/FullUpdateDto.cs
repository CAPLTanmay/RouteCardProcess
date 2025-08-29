using System.Text.Json.Serialization;

namespace RouteCardProcess.Model.DTOs.RouteCardReport
{
    public class FullUpdateDto
    {
        public string? setupOperatorId { get; set; }
        public string? machiningOperatorId { get; set; }
        public SetupUpdateDto Setup { get; set; }
        public MachiningUpdateDto Machining { get; set; }
    }

    public class SetupUpdateDto
    {
        public string SetUpID { get; set; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public string? OperatorTransactionId { get; set; }
        public DateTime? setupOperatorStartDate { get; set; }
        public TimeSpan? setupOperatorStartTime { get; set; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? setupOperatorEndDate { get; set; }
        public TimeSpan? setupOperatorEndTime { get; set; }
        public string UpdatedOperatorId { get; set; }
        public List<IdleTimeUpdateDto> IdleTimes { get; set; } = new();
        public List<ExceptionTimeUpdateDto> ExceptionTimes { get; set; } = new();
        // Computed properties to combine Date + Time
       
        [JsonIgnore]
        public DateTime? OperatorStartDateTime =>
       (setupOperatorStartDate.HasValue && setupOperatorStartTime.HasValue)
           ? setupOperatorStartDate.Value.Date + setupOperatorStartTime.Value
           : null;

        [JsonIgnore]
        public DateTime? OperatorEndDateTime =>
            (setupOperatorEndDate.HasValue && setupOperatorEndTime.HasValue)
                ? setupOperatorEndDate.Value.Date + setupOperatorEndTime.Value
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
   
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? machiningOperatorStartDate { get; set; }
        public TimeSpan? machiningOperatorStartTime { get; set; }

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? machiningOperatorEndDate { get; set; }
        public TimeSpan? machiningOperatorEndTime { get; set; }
        public string UpdatedOperatorId { get; set; }

        public List<MachiningIdleTimeUpdateDto> IdleTimes { get; set; } = new();
        public List<MachiningExceptionUpdateDto> ExceptionTimes { get; set; } = new();
        public List<MachiningOperatorQtyUpdateDto> OperatorQuantities { get; set; } = new();

        // Combine Date + Time for DB use
        [JsonIgnore]
        public DateTime? MachiningOpertorStartDateTime =>
            (machiningOperatorStartDate.HasValue && machiningOperatorStartTime.HasValue)
                ? machiningOperatorStartDate.Value.Date + machiningOperatorStartTime.Value
                : null;

        [JsonIgnore]
        public DateTime? MachiningOpertorEndDateTime =>
            (machiningOperatorEndDate.HasValue && machiningOperatorEndTime.HasValue)
                ? machiningOperatorEndDate.Value.Date + machiningOperatorEndTime.Value
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
