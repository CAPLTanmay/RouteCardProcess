using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Department
{
    public class DepartmentMasterDto
    {
        public int DepartmentId { get; set; }

        [Required]
        public string DepartmentName { get; set; }
    }
}
