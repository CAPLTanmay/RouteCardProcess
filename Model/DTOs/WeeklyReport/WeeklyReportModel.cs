using System.Text.Json.Serialization;

namespace RouteCardProcess.Model.DTOs.WeeklyReport
{
    public class WeeklyExceptionReportModel
    {
        [JsonPropertyName("CONF_DATE")]
        public DateTime? ConfDate { get; set; }

        [JsonPropertyName("PRO_ODER")]
        public string? ProOder { get; set; }

        [JsonPropertyName("OPRTION_NO")]
        public string? OprtionNo { get; set; }

        [JsonPropertyName("WORK_CENTER")]
        public string? WorkCenter { get; set; }

        [JsonPropertyName("Oprtorcode")]
        public string? OprtorCode { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Department")]
        public string? Col5 { get; set; }

        [JsonPropertyName("MATERIAL")]
        public string? Material { get; set; }

        [JsonPropertyName("Maktx")]
        public string? Maktx { get; set; }

        [JsonPropertyName("OPRTION_DESC")]
        public string? OprtionDesc { get; set; }

        [JsonPropertyName("Exception_Code")]
        public string? ExceptionCode { get; set; }

        [JsonPropertyName("Exception_Desc")]
        public string? ExceptionDesc { get; set; }

        [JsonPropertyName("Remark")]
        public string? Remark { get; set; }

        [JsonPropertyName("OPRTION_QTY")]
        public decimal? OprtionQty { get; set; }

        [JsonPropertyName("YIELD_QTY")]
        public decimal? YieldQty { get; set; }

        [JsonPropertyName("PROCES_TIME")]
        public decimal? ProcesTime { get; set; }

        [JsonPropertyName("plantime")]
        public decimal? PlanTime { get; set; }

        [JsonPropertyName("STD_MACH_UP")]
        public decimal? StdMachUp { get; set; }

        [JsonPropertyName("STD_lAB_UP")]
        public decimal? StdLabUp { get; set; }

        [JsonPropertyName("ExcessTime")]
        public decimal? ExcessTime { get; set; }

        [JsonPropertyName("comp_Status")]
        public string? CompStatus { get; set; }
    }
}
