using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Setup
{
    public class SetupIdentifierRequest
    {
        [Required]
        public string SetUpID { get; set; }
    }
}
