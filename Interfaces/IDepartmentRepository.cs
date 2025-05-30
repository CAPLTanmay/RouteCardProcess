using RouteCardProcess.Model.DTOs.Department;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<IEnumerable<DepartmentMaster>> GetAllAsync();
        Task<int> AddAsync(DepartmentMasterDto department);
    }
}
