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
        public DateTime? SetupStartTime { get; set; }
        public DateTime? SetupEndTime { get; set; }
        public int UpdatedOperatorId { get; set; }

        public List<IdleTimeUpdateDto> IdleTimes { get; set; } = new();
        public List<ExceptionTimeUpdateDto> ExceptionTimes { get; set; } = new();
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
        public DateTime? MachiningStartTime { get; set; }
        public DateTime? MachiningEndTime { get; set; }
        public int UpdatedOperatorId { get; set; }

        public List<MachiningIdleTimeUpdateDto> IdleTimes { get; set; } = new();
        public List<MachiningExceptionUpdateDto> ExceptionTimes { get; set; } = new();
        public List<MachiningOperatorQtyUpdateDto> OperatorQuantities { get; set; } = new();
    }

    public class MachiningIdleTimeUpdateDto
    {
        public string MSTIdleCode { get; set; }
        public TimeSpan NewMachiningIdleTime { get; set; }
    }

    public class MachiningExceptionUpdateDto
    {
        public string StdExceptionsReasonCode { get; set; }
        public string ExceptionsReasonCode { get; set; }
        public TimeSpan NewExceptionsTime { get; set; }
    }

    public class MachiningOperatorQtyUpdateDto
    {
        public int OperatorId { get; set; }
        public int NewCompletedQty { get; set; }
    }

}
