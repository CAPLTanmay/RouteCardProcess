using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IMstWorkCenterRepository
    {
        Task<int> AddMstWorkCenterAsync(MstWorkCenterRequest request);
        Task<int> UpdateMstWorkCenterAsync(MstWorkCenterRequest request);
        Task<IEnumerable<MstWorkCenterRequest>> GetAllMstWorkCentersAsync();
        Task<IEnumerable<string>> GetDistinctDepartmentsAsync();
        Task<IEnumerable<MstWorkCenterRequest>> GetWorkCentersByDeptAsync(string dept);
    }
}
