using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RouteCardProcess.Model.DTOs.RouteCardReport
{
    public class WorkOrderRequest
    {
        [Required]
        public string WorkOrderNo { get; set; }
    }

    public class OrderReportRequestDto
    {
        public string? SetupId { get; set; }
        public string? MachiningId { get; set; }
    }


    public class RouteCardReportFilterRequest
    {
        public string? OperatorId { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public string? ProductionOrderNo { get; set; }
        public string? Department { get; set; }
        public string? WorkCenterNo { get; set; }
    }

    public class RouteCardReportDto
    {
        public string OperatorName { get; set; }
        public string CurrentShift { get; set; }
        public string OperatorId { get; set; }
        public string ProductionOrderNo { get; set; }
        public string WorkCenterNo { get; set; }
        public string WorkCenterText { get; set; }
        public string Material { get; set; }
        public string MaterialText { get; set; }

        public string MrpController { get; set; }
        public string ProductionScheduler { get; set; }
        public string ProcessingUnit { get; set; }
        public string ProductionUnit { get; set; }
        public string OperationNo { get; set; }
        public string OperationDescription { get; set; }
        public string OrderType { get; set; }
        public int TotalQty { get; set; }
        public int Pending_qty { get; set; }
        public int CompletedQty { get; set; }
        public string SetupId { get; set; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime SetupStartDate { get; set; }
        public TimeSpan SetupStartTime { get; set; } // for the TIME part 

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime SetupEndDate { get; set; }
        public TimeSpan SetupEndTime { get; set; } // for the TIME part 

        public TimeSpan StandardSetupTime { get; set; }
        public int ActualSetupTime { get; set; }
        public string ActualSetupTime_HHMMSS { get;set;}
        public int TotalSetupIdleMinutes { get; set; }
        public string TotalSetupIdle_HHMMSS { get; set; }

        public int TotalSetupExceptionsMinutes { get; set; }
        public string TotalSetupExceptions_HHMMSS { get; set; }

        public DateTime? SetupOperatorStartTime { get; set; }
        public DateTime? SetupOperatorEndTime { get; set; }
        public string MachiningId { get; set; }
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime MachiningStartDate { get; set; }
        public TimeSpan MachiningStartTime { get; set; } // for the TIME part 

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime MachiningEndDate { get; set; }
        public TimeSpan MachiningEndTime { get; set; } // for the TIME part 
        public TimeSpan StandardMachiningTime { get; set; }
        public int ActualMachiningTime { get; set; }
        public string ActualMachiningTime_HHMMSS { get; set; }
        public int TotalMachiningIdleMinutes { get; set; }
        public string TotalMachiningIdle_HHMMSS { get; set; }

        public int TotalMachiningExceptionsMinutes { get; set; }
        public string TotalMachiningExceptions_HHMMSS { get; set; }

        public DateTime? MachiningOperatorStartTime { get; set; }
        public DateTime? MachiningOperatorEndTime { get; set; }
        public int ActualOperationTime { get; set; }
        public int IdleOperationTime { get; set; }
        public int ExceptionOperationTime { get; set; }
        public DateTime? FinishDate { get; set; }
        public int ActualLaborTime { get; set; }
        public decimal ActualLaborTime_Hours { get; set; }
    }

    public class LossOrderResponseDto
    {
        public string ORDER { get; set; }
        public List<SetupIdleDto> SetupIdleRecords { get; set; }
        public List<MachiningIdleDto> MachiningIdleRecords { get; set; }
    }
    public class SetupIdleDto
    {
        public string SetUpID { get; set; }
        public string OperatorId { get; set; }
        public string ORDER { get; set; }
        public string MSTIdleCode { get; set; }
        public TimeSpan SetupIdleTime { get; set; }
    }

    public class MachiningIdleDto
    {
        public string MachiningID { get; set; }
        public string OperatorId { get; set; }
        public string ORDER { get; set; }
        public string MSTIdleCode { get; set; }
        public TimeSpan MachiningIdleTime { get; set; }
    }


    public class ExceptionRecordDto
    {
        public string OperatorId { get; set; }
        public string ExceptionsReasonCode { get; set; }
        public string StdExceptionsReasonCode { get; set; }
        public TimeSpan ExceptionsTime { get; set; }
        public string StdExceptionsRemark { get; set; }
    }

    public class ExceptionReportResponseDto
    {
        public string? SetupId { get; set; }
        public string? MachiningId { get; set; }
        public List<ExceptionRecordDto> SetupExceptions { get; set; } = new();
        public List<ExceptionRecordDto> MachiningExceptions { get; set; } = new();
    }

    public class DateOnlyConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-dd";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.ParseExact(reader.GetString()!, Format, null);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(Format));
        }
    }
    public class CombinedOrderReportResponseDto
    {
        public TimingInfoDto? TimingInfo { get; set; }
        public LossOrderResponseDto? NavLossData { get; set; }
        public ExceptionReportResponseDto? ExceptionReportData { get; set; }
    }

    public class TimingInfoDto
    {
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? SetupStartDate { get; set; }

        public TimeSpan? SetupStartTime { get; set; }

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? SetupEndDate { get; set; }

        public TimeSpan? SetupEndTime { get; set; }

        public TimeSpan? StandardSetupTime { get; set; }
        public TimeSpan? TotalSetupTime { get; set; }

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? MachiningStartDate { get; set; }

        public TimeSpan? MachiningStartTime { get; set; }

        [JsonConverter(typeof(DateOnlyConverter))]
        public DateTime? MachiningEndDate { get; set; }

        public TimeSpan? MachiningEndTime { get; set; }

        public TimeSpan? StandardMachiningTime { get; set; }
        public TimeSpan? TotalMachiningTime { get; set; }
    }


}
