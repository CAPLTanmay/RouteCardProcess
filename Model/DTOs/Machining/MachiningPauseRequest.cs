using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Machining
{
    public class MachiningPauseRequest
    {
        [Required]
        public string MachiningId { get; set; } = string.Empty;

        public string? PauseCode { get; set; }
    }
}
