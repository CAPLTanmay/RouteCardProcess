namespace RouteCardProcess.Model.DTOs.Employee
{
    public class EmployeeRequest
    {
        public int EmployeeId { get; set; }
        public int? EmployeeCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserRole { get; set; }
        public string? UserDepartment { get; set; }
        public bool IsContractEmployee { get; set; }
        public string? EmployeePassword { get; set; }
        public DateTime? EmployeeStartDate { get; set; }
        public DateTime? EmployeeEndDate { get; set; }
        public bool? IsTempPassword { get; set; }
        public int CreatedBy { get; set; }
        public List<EmployeeDept> DepartmentIds { get; set; } = new();
    }
    public class EmployeeDept
    {
        public string? DepartmentName { get; set; }
        public int DepartmentId { get; set; }
    }

    public class UserDepartmentMapping
    {
        public int UserId { get; set; }
        public string? DepartmentName { get; set; }  
        public int DepartmentId { get; set; }
    }
}