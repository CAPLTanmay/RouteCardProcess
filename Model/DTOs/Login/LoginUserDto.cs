namespace RouteCardProcess.Model.DTOs.Login
{
    public class LoginUserDto
    {
        public int SrNo { get; set; }
        public string OperatorId { get; set; }
        public string ContractEmpId { get; set; }
        public string OperatorName { get; set; }
        public string OperatorRole { get; set; }
        public string DepartmentName { get; set; }
        public int DepartmentId { get; set; }
        public string Shift { get; set; }
        public bool IsFromKBL { get; set; }
        public List<string> RBACDepartmentName { get; set; }
    }
}
