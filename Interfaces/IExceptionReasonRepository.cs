using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IExceptionReasonRepository
    {
        Task<int> AddExceptionReasonAsync(ExceptionReasonRequest request);
        Task<int> UpdateExceptionReasonAsync(ExceptionReasonRequest request);
        Task<IEnumerable<ExceptionReasonRequest>> GetAllExceptionReasonsAsync();
        Task<int> DeleteExceptionReasonAsync(string reason_Code);
    }
}
