using RouteCardProcess.Model.DTOs.Machining;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface IMachiningRepository
    {
        Task<MachiningMaster> CreateAsync(MachiningDto obj);
        Task InsertMachiningOperatorStartAsync(MachiningOperatorStartRequest request);
        Task<MachiningStartResponse> StartMachiningAsync(MachiningIdentifierRequest request);
        Task TogglePauseAsync(MachiningPauseRequest request);
        Task<EndMachiningResultDto> EndMachiningAsync(MachiningIdentifierRequest request);
        Task AddQuantitiesAsync(AddQuantityRequest request);
        Task<ProcessQuantityResponse> ProcessQuantitiesAsync(AddQuantity request);
        Task<bool> AddDelaysAsync(MachiningDelayRequest request);
        Task<MachiningMaster> GetByCompositeKeyAsync(CompositeKeyRequest request);
        Task UpdateMachiningStatusAsync(MachiningIdentifierRequest request);
    }
}
