using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Setup
{
    public class SetupDelayRequest
    {
        [Required]
        public string SetUpID { get; set; }

        [Required]
        public string SetUpStatus { get; set; }

        [Required]
        public List<DelayRequest> Delays { get; set; }
    }

    public class DelayRequest
    {
        [Required]
        public string DelayReasonCode { get; set; }

        [Required]
        public TimeSpan DelayTime { get; set; }
    }
}
