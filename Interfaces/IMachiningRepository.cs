using RouteCardProcess.Model.DTOs.Machining;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IMachiningRepository
    {
        Task<MachiningMaster> CreateAsync(MachiningDto obj);
        Task<string> StartMachiningAsync(string machiningId);
        Task TogglePauseAsync(string machiningId, string pauseCode);
        Task<bool> EndMachiningAsync(string machiningId);
        Task AddQuantitiesAsync(string machiningId, int totalQty, int processedQty, string qtyStatus);
        Task ProcessQuantitiesAsync(AddQuantity request);
        Task<bool> AddDelaysAsync(MachiningDelayRequest request);
        Task<MachiningMaster> GetByCompositeKeyAsync(string workCenterNo, string workOrderNo, string operationNo);
        Task UpdateMachiningStatusAsync(string machiningId);

    }
}
