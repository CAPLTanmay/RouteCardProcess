using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Setup
{
    public class SetupPauseRequest
    {
        [Required]
        public string SetUpID { get; set; }

        public string? PauseCode { get; set; }
    }
}
