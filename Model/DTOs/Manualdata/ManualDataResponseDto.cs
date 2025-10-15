using System;

namespace RouteCardProcess.Model.DTOs.ManualData
{
    public class ManualDataResponseDto
    {
        public int OperatorId { get; set; }
        public string WorkOrder { get; set; }
        public string WorkCenter { get; set; }
        public string OperationNo { get; set; }
        public string SetupId { get; set; }
        public string MachiningId { get; set; }
        public int TotalQty { get; set; }
        public int S_ConfirmedQuantity { get; set; }
        public int L_CompletedQty { get; set; }
        public int PendingQty { get; set; }
        public DateTime? SetupStartTime { get; set; }
        public DateTime? SetupEndTime { get; set; }
        public int? ActualSetupTime { get; set; }
        public TimeSpan? IdleSetupTime { get; set; }
        public TimeSpan? SetupExceptionTime { get; set; }
        public DateTime? MachiningStartTime { get; set; }
        public DateTime? MachiningEndTime { get; set; }
        public int? ActualMachiningTime { get; set; }
        public TimeSpan? IdleMachiningTime { get; set; }
        public TimeSpan? MachiningExceptionTime { get; set; }
        public DateTime? OperationStartTime { get; set; }
        public DateTime? OperationEndTime { get; set; }
        public TimeSpan? ActualOperationTime { get; set; }
        public TimeSpan? Total_Ideal_Time { get; set; }
        public TimeSpan? Total_Exception_Time { get; set; }
        public string S_RoutingDataStatus { get; set; }
        public string OperationDescription { get; set; }
        public string WorkCenterText { get; set; }
        public string SetupUnit { get; set; }
        public string ProcessingUnit { get; set; }
        public string Material { get; set; }
        public string MaterialText { get; set; }
        public string OrderType { get; set; }
        public string OrderTypeDesc { get; set; }
        public string ProductionPlant { get; set; }
        public string ProductionUnit { get; set; }
        public string MrpController { get; set; }
        public string ProductionScheduler { get; set; }
        public string ControlKey { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public int? Std_SetupTime { get; set; }
        public int? Std_Machining_Time { get; set; }
        public string? MaterialTextLink { get; set; }

        public string SetupStartDate => SetupStartTime?.ToString("yyyy-MM-dd") ?? "-";
        public string SetupStartClock => SetupStartTime?.ToString("HH:mm:ss") ?? "-";

        public string SetupEndDate => SetupEndTime?.ToString("yyyy-MM-dd") ?? "-";
        public string SetupEndClock => SetupEndTime?.ToString("HH:mm:ss") ?? "-";

        public string MachiningStartDate => MachiningStartTime?.ToString("yyyy-MM-dd") ?? "-";
        public string MachiningStartClock => MachiningStartTime?.ToString("HH:mm:ss") ?? "-";

        public string MachiningEndDate => MachiningEndTime?.ToString("yyyy-MM-dd") ?? "-";
        public string MachiningEndClock => MachiningEndTime?.ToString("HH:mm:ss") ?? "-";
    }
}
