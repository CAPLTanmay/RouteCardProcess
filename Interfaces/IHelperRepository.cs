using RouteCardProcess.Model.DTOs.Helper;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IHelperRepository
    {
        Task<string> AddHelperAsync(HelperRequest request);
        Task<string> EndHelperAsync(EndHelperRequest request);
        Task<string> ToggleHelperPauseAsync(EndHelperRequest request);
        Task<IEnumerable<OperatorHelperLog>> GetHelpersByMainOperatorIdAsync(string mainOperatorId);
    }
}
