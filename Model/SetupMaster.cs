using System.ComponentModel.DataAnnotations.Schema;

namespace RouteCardProcess.Model
{
    public class SetupMaster
    {
        public string ?SetUpID { get; set; }
        public string WorkCenterNo { get; set; }
        public string WorkOrderNo { get; set; }
        public string OperationNo { get; set; }
        public string OperatorId { get; set; }

        public TimeSpan? IdealTime { get; set; }
        public string ?SetupStatus { get; set; }
        public DateTime OperatorStartTime { get; set; }
        public DateTime OperatorEndTime { get; set; }
    }

    public class SetupIdentifierRequest
    {
        public string SetUpID { get; set; }
    }
    public class SetupPauseRequest
    {
        public string SetUpID { get; set; }
        public string? PauseCode { get; set; }
    }

    public class SetupDelayRequest
    {
        public string SetUpID { get; set; }
        public string SetUpStatus { get; set; }
        public List<DelayRequest> Delays { get; set; }
    }


    public class DelayRequest
    {
        public string DelayReasonCode { get; set; }
        public TimeSpan DelayTime { get; set; }
    }
}
