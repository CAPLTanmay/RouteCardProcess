using System.Text.Json.Serialization;

namespace RouteCardProcess.Model.DTOs.SapValidation
{
    public class WorkCenterUpdateRequest
    {
        [JsonPropertyName("ORDER_NUMBER")]
        public string ORDER_NUMBER { get; set; }

        [JsonPropertyName("WORK_CENTER")]
        public string WORK_CENTER { get; set; }

        [JsonPropertyName("OLD_WORKCENTER")]
        public string OLD_WORKCENTER { get; set; }

        [JsonPropertyName("OPERATION")]
        public string OPERATION { get; set; }

        [JsonPropertyName("WORK_CENTER_TEXT")]
        public string? WORK_CENTER_TEXT { get; set; }

        [JsonPropertyName("QUANTITY")]
        public string? QUANTITY { get; set; }
    }

    public class SAPBreakdownStatusResponse
    {
        public string NOTIF_NUM { get; set; }
        public string STATUS { get; set; }
        public string NOTIF_CLOSE_DATE { get; set; }
        public string NOTIF_CLOSE_TIME { get; set; }
    }


}