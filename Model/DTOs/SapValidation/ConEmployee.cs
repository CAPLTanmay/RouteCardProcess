namespace RouteCardProcess.Model.DTOs.SapValidation
{
    public class ConEmployee
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserRole { get; set; }
        public string UserDepartment { get; set; }
        public bool IsContractEmployee { get; set; }
        public string EmployeePassword { get; set; }
        public DateTime? EmployeeStartDate { get; set; }
        public DateTime? EmployeeEndDate { get; set; }
        public bool IsTempPassword { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public string ContractEmpId { get; set; }
    }
}
