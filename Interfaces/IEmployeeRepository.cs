using RouteCardProcess.Model.DTOs.Employee;
using RouteCardProcess.Model.DTOs.RBACEmployee;

namespace RouteCardProcess.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<string> AddEmployeeAsync(EmployeeRequest request);
        Task<string> UpdateEmployeeAsync(UpdateEmployeeRequest request);
        Task<IEnumerable<EmployeeResponse>> GetAllEmployeesAsync();
        Task<EmployeeResponse?> GetEmployeeByIdAsync(GetEmployeeRequest request);
        Task<string> SoftDeleteEmployeeAsync(int employeeId, int updatedBy);
        Task<bool> ResetTempPasswordAsync(ResetPasswordDto request);
        Task<int> InactivateExpiredContractEmployeesAsync(DateTime? asOfDate = null);
    }
}
