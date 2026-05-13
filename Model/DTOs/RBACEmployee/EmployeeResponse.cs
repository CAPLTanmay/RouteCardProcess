namespace RouteCardProcess.Model.DTOs.Employee
{
    public class EmployeeResponse
    {
        public int EmployeeId { get; set; }
        public int EmployeeCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserRole { get; set; }
        public string? UserDepartment { get; set; }
        public bool IsContractEmployee { get; set; }
        public string? ContractEmpId { get; set; }
        public string? EmployeePassword { get; set; }
        public DateTime? EmployeeStartDate { get; set; }
        public DateTime? EmployeeEndDate { get; set; }
        public bool? IsTempPassword { get; set; }
        public bool IsActive { get; set; }
        public List<DepartmentDto> Departments { get; set; } = new List<DepartmentDto>();
    }
    public class DepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }

}
