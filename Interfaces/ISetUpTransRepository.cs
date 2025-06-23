using RouteCardProcess.Model.DTOs.Setup;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Interfaces
{
    public interface ISetUpTransRepository
    {
        Task<SetupMaster> GetByCompositeKeyAsync(string workCenterNo, string workOrderNo, string operationNo);

        Task<(int Flag, string SetupStatus, string MachiningStatus, string Message, string SetUpID, string MachiningID)>
        CheckSetupNotificationStatusAsync(string workCenterNo, string workOrderNo, string operationNo);

        Task<SetupMaster> CreateSetupAsync(SetupMasterDto request);

        Task<string> StartSetupAsync(string setUpId);

        Task<string> TogglePauseAsync(SetupPauseRequest request);

        Task<bool> EndSetupTimeAsync(string setUpId);

        Task<bool> InsertDelaysAsync(SetupDelayRequest request);
        Task InsertSetupOperatorStartAsync(string setupId, string operatorId, DateTime startTime);
    }
}
