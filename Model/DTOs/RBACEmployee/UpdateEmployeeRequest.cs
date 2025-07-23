using RouteCardProcess.Model.DTOs.Employee;

public class UpdateEmployeeRequest : EmployeeRequest
{
    public int EmployeeId { get; set; }
    public int? UpdatedBy { get; set; }
}
