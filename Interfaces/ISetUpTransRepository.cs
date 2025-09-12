using RouteCardProcess.Model.DTOs.Setup;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface ISetUpTransRepository
    {
        Task<SetupMaster> GetByCompositeKeyAsync(SetupCompositeKeyRequest request);
        Task<(int Flag, string SetupStatus, string MachiningStatus, string Message, string SetUpID, string MachiningID, bool Breakdown)>
        CheckSetupNotificationStatusAsync(string workCenterNo, string workOrderNo, string operationNo);
        Task<SetupMaster> CreateSetupAsync(SetupMasterDto request);
        Task<SetupStartResponse> StartSetupAsync(SetupIdentifierRequest request);
        Task<string> TogglePauseAsync(SetupPauseRequest request);
        Task<bool> EndSetupTimeAsync(SetupIdentifierRequest request);
        Task<bool> InsertDelaysAsync(SetupDelayRequest request);
        Task InsertSetupOperatorStartAsync(string setupId, string operatorId, DateTime startTime);
    }
}
