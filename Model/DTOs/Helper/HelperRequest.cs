using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Helper
{
    public class HelperRequest
    {
        [Required]
        public string OperatorId { get; set; }

        [Required]
        public string Password { get; set; }
        [Required]
        public string MainOperatorId { get; set; }
        public string? SetupId { get; set; }
        public string? MachiningId { get; set; }
        public string? MSTIdleCode { get; set; }
        public string? WorkCenter { get; set; }
    }

    public class EndHelperRequest
    {
        [Required]
        public string OperatorId { get; set; }

        public string? SetupId { get; set; }

        public string? MachiningId { get; set; }
    }

    public class MainOperatorRequestDto
    {
        public string MainOperatorId { get; set; }
    }

}
