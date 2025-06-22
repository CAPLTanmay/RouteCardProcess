using System.ComponentModel.DataAnnotations;

namespace RouteCardProcess.Model.Entities
{
    public class LogInMaster
    {
        public int SrNo { get; set; }
        public string OperatorId { get; set; }
        public string OperatorName { get; set; }
        public string OperatorPassword { get; set; }
        public string OperatorRole { get; set; }
        public string DepartmentName { get; set; }
        public int DepartmentId { get; set; }
        public string Shift { get; set; }
        public bool IsFromKBL { get; set; }
        public string? OperatorDummyID { get;  set; }
    }
}
