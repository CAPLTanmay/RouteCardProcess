using System.Text.Json.Serialization;

namespace RouteCardProcess.Model.DTOs.WeeklyReport
{
    public class IdleReportModel
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }             // Operator full name

        [JsonPropertyName("Department")]
        public string Department { get; set; }       // Dept from MSTLossOrder

        [JsonPropertyName("CDATE")]
        public DateTime CDATE { get; set; }          // OperatorEndTime

        [JsonPropertyName("PRO_ODER")]
        public string PRO_ODER { get; set; }         // Production Order No

        [JsonPropertyName("OPRTION_NO")]
        public string OPRTION_NO { get; set; }       // Operation No

        [JsonPropertyName("WORK_CENTER")]
        public string WORK_CENTER { get; set; }      // Work Center

        [JsonPropertyName("Operator")]
        public int Operator { get; set; }            // Operator Id

        [JsonPropertyName("TYP_Ideal_code")]
        public string TYP_Ideal_code { get; set; }   // Type Idle Code

        [JsonPropertyName("TYP_Ideal_Text")]
        public string TYP_Ideal_Text { get; set; }   // Type Idle Desc

        [JsonPropertyName("Ideal_Code")]
        public string Ideal_Code { get; set; }       // Idle Code from transaction

        [JsonPropertyName("Ideal_Reason")]
        public string Ideal_Reason { get; set; }     // Idle Reason

        [JsonPropertyName("Ideal_time")]
        public int Ideal_time { get; set; }          // Minutes
    }
}
