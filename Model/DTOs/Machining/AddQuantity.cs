using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Machining
{
    public class AddQuantity
    {
        [Required]
        public string MachiningId { get; set; } = string.Empty;
        public List<QuantityList> QuantityList { get; set; } = new();
    }

    public class QuantityList
    {
        public string MachiningStatus { get; set; } = string.Empty;
        public string ProcessedQty { get; set; } = string.Empty;
    }
}
