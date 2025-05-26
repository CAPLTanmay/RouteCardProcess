using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model
{
    public class LogInMaster
    {
        public int SrNo { get; set; }

        [Required(ErrorMessage = "OperatorId is required")]
        [StringLength(50)]
        public string OperatorId { get; set; }

        [Required(ErrorMessage = "OperatorName is required")]
        [StringLength(100)]
        public string OperatorName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [StringLength(50)]
        public string Role { get; set; }

        [Required(ErrorMessage = "DepartmentName is required")]
        [StringLength(100)]
        public string DepartmentName { get; set; }

        public int DepartmentId { get; set; }
        public string Shift { get; set; }
        public bool IsFromKBL { get; set; }

    }
}
