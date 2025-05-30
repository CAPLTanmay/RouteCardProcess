using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Setup
{
    public class SetupPauseRequest
    {
        [Required]
        public string SetUpID { get; set; }

        [Required]
        public string PauseCode { get; set; }
    }
}
