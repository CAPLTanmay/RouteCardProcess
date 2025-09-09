using RouteCardProcess.Model.DTOs.Login;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface ILogInRepository
    {
        Task<IEnumerable<LogInMaster>> GetAllAsync();
        Task<int> AddAsync(LogInMaster login);
        Task<LogInMaster?> ValidateLoginAsync(string operatorId, string password);
        Task<LoginResult> LoginEmployeeAsync(string operatorId, string password);
        Task<(int Flag, string Message)> TryLogoutAsync(string workCenterNo, string workOrderNo, string operationNo);
        Task<ShiftDto> GetCurrentShiftAsync(); 
    }
}
