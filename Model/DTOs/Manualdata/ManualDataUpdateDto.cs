using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RouteCardProcess.Middleware;
using RouteCardProcess.Model.DTOs.Machining;
using RouteCardProcess.Model.DTOs.Setup;

namespace RouteCardProcess.Model.DTOs.Manualdata
{
    // --------------------------
    //  Manual Data Update
    // --------------------------
    public class ManualDataUpdateDto
    {
        [Required]
        [SafeText(SafeTextPattern.AlphaNumeric, 20)]
        public string WorkOrder { get; set; }

        [Required]
        [SafeText(SafeTextPattern.AlphaNumeric, 20)]
        public string WorkCenter { get; set; }

        [Required]
        [SafeText(SafeTextPattern.AlphaNumeric, 10)]
        public string OperationNo { get; set; }

        [Required]
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public int OperatorId { get; set; }
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public int? L_CompletedQty { get; set; }

        public DateTime? SetupStartTime { get; set; }
        public DateTime? SetupEndTime { get; set; }
        public DateTime? MachiningStartTime { get; set; }
        public DateTime? MachiningEndTime { get; set; }
    }

    // --------------------------
    // Manual Setup Delay
    // --------------------------
    public class ManualSetupDelayRequest
    {
        [Required]
        [SafeText(SafeTextPattern.AlphaNumeric, 20)]
        public string SetUpID { get; set; }

        [Required]
        [SafeText(SafeTextPattern.AlphaNumericWithSymbols, 50)]
        public string SetUpStatus { get; set; }

        [Required]
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public int OperatorId { get; set; }

        [Required]
        public List<ExceptionsRequest> Exceptions { get; set; } = new();

        [Required]
        public List<IdleTimeRequest> IdleTimes { get; set; } = new();
    }

    // --------------------------
    // Manual Machining Delay
    // --------------------------
    public class ManualMachiningDelayRequest
    {
        [Required]
        [SafeText(SafeTextPattern.AlphaNumeric, 20)]
        public string MachiningId { get; set; }

        [Required]
        [SafeText(SafeTextPattern.NumericOnly, 10)]
        public int OperatorId { get; set; }

        [Required]
        public List<MachiningExceptionsRequest> Exceptions { get; set; } = new();

        [Required]
        public List<MachiningIdleTimeRequest> IdleTimes { get; set; } = new();
    }
}
