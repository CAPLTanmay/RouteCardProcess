using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.DTOs.Department
{
    public class DepartmentMasterDto
    {
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "DepartmentName is required")]
        [StringLength(100, ErrorMessage = "DepartmentName cannot exceed 100 characters")]
        public string DepartmentName { get; set; }
    }
}
