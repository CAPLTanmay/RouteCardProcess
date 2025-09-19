using System.Text.Json.Serialization;

namespace RouteCardProcess.Model.DTOs.WeeklyReport
{
    public class AssociateReportModel
    {
        [JsonPropertyName("Department")]
        public string Department { get; set; }
        [JsonPropertyName("TotalCount")]
        public int TotalCount { get; set; }
    }
}
