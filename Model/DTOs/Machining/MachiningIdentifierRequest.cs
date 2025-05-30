using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Machining
{
    public class MachiningIdentifierRequest
    {
        [Required]
        public string MachiningId { get; set; } = string.Empty;
    }
}
