using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.Entities
{
    public class LogInMaster
    {
        public int SrNo { get; set; }
        public string OperatorId { get; set; }
        public string OperatorName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string DepartmentName { get; set; }
        public int DepartmentId { get; set; }
        public string Shift { get; set; }
        public bool IsFromKBL { get; set; }
    }
}
