using RouteCardProcess.Model.DTOs.Machining;
using RouteCardProcess.Model.DTOs.Setup;

namespace RouteCardProcess.Model.DTOs.Manualdata
{
    public class ManualDataUpdateDto
    {
        public string WorkOrder { get; set; }
        public string WorkCenter { get; set; }
        public string OperationNo { get; set; }

        public int OperatorId { get; set; }
        public int? L_CompletedQty { get; set; }

        public DateTime? SetupStartTime { get; set; }
        public DateTime? SetupEndTime { get; set; }
        public DateTime? MachiningStartTime { get; set; }
        public DateTime? MachiningEndTime { get; set; }
    }

    public class ManualSetupDelayRequest
    {
        public string SetUpID { get; set; }
        public string SetUpStatus { get; set; }
        public int OperatorId { get; set; }
        public List<ExceptionsRequest> Exceptions { get; set; }
        public List<IdleTimeRequest> IdleTimes { get; set; }
    }

    public class ManualMachiningDelayRequest
    {
        public string MachiningId { get; set; }
        public int OperatorId { get; set; }
        public List<MachiningExceptionsRequest> Exceptions { get; set; }
        public List<MachiningIdleTimeRequest> IdleTimes { get; set; }
    }
}
