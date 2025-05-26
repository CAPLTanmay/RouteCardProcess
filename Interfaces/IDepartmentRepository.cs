using RouteCardProcess.Model;

namespace RouteCardProcess.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<IEnumerable<DepartmentMaster>> GetAllAsync();
        Task<int> AddAsync(DepartmentMaster department);
    }
}
