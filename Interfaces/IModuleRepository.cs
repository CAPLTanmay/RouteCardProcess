using RouteCardProcess.Model.DTOs.Module;

namespace RouteCardProcess.Interfaces
{
    public interface IModuleRepository
    {
        Task<IEnumerable<ModuleResponseDto>> GetAllModulesAsync();
    }
}
