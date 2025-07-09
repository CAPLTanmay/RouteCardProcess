using System.Text.Json.Serialization;

namespace RouteCardProcess.Model.DTOs.SapValidation
{
    public class ProductionOrderConfirmationRequest
    {
        [JsonPropertyName("ORDER")]
        public string ORDER { get; set; }

        [JsonPropertyName("NAV_CONF")]
        public List<NavConfirmation> NAV_CONF { get; set; }
    }

    public class NavConfirmation
    {
        [JsonPropertyName("ORDER")]
        public string ORDER { get; set; }

        [JsonPropertyName("WORKCENTER")]
        public string WORKCENTER { get; set; }

        [JsonPropertyName("OPERATION")]
        public string OPERATION { get; set; }

        [JsonPropertyName("QTY")]
        public string QTY { get; set; }

        [JsonPropertyName("ACT_SETUP_TIME")]
        public string ACT_SETUP_TIME { get; set; }

        [JsonPropertyName("SETUP_FIN_DATE")]
        public string SETUP_FIN_DATE { get; set; }

        [JsonPropertyName("ACT_MACHINE_TIME")]
        public string ACT_MACHINE_TIME { get; set; }

        [JsonPropertyName("EXE_FIN_DATE")]
        public string EXE_FIN_DATE { get; set; }

        [JsonPropertyName("PERS_NO")]
        public string PERS_NO { get; set; }

        [JsonPropertyName("SHIFT")]
        public string SHIFT { get; set; }

        [JsonPropertyName("FULL_CONF_FLAG")]
        public string FULL_CONF_FLAG { get; set; }
    }
    public class SAPBreakdownRequest
    {
        public string WORKCENTER { get; set; } = "";
        public string EQUIPMENT { get; set; } = "";
        public string CODE_GRP { get; set; } = "";
        public string CODE { get; set; } = "";
        public string BRKDWN_DATE { get; set; } = "";
        public string BRKDWN_TIME { get; set; } = "";
        public string NOTIF_NUM { get; set; } = "";
    }
    public class SAPBreakdownEnvelope
    {
        public SAPBreakdownRequest d { get; set; }
    }
    public class SAPBreakdownCloseRequest
    {
        public string NOTIF_NUM { get; set; } = "";
        public string STATUS { get; set; } = " ";
    }
}
