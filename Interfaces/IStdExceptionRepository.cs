using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IStdExceptionRepository
    {
        Task<int> AddStdExceptionAsync(StdExceptionRequest request);
        Task<int> UpdateStdExceptionAsync(StdExceptionRequest request);
        Task<IEnumerable<StdExceptionRequest>> GetAllStdExceptionsAsync();
        Task<int> DeleteStdExceptionAsync(string reasonCode);

    }
}
