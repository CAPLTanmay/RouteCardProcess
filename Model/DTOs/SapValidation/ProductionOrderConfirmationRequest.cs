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

        [JsonPropertyName("SHIFT_NAME")]
        public string SHIFT_NAME { get; set; }

        [JsonPropertyName("FULL_CONF_FLAG")]
        public string FULL_CONF_FLAG { get; set; }

        [JsonPropertyName("ORDER_TYPE")]
        public string ORDER_TYPE { get; set; }

        [JsonPropertyName("MATERIAL")]
        public string MATERIAL { get; set; }

        [JsonPropertyName("MATERIAL_DESC")]
        public string MATERIAL_DESC { get; set; }

        [JsonPropertyName("OPR_TXT")]
        public string OPR_TXT { get; set; }

        [JsonPropertyName("OPERATOR_NAME")]
        public string OPERATOR_NAME { get; set; }

        [JsonPropertyName("OPR_WORK_MINS")]
        public string OPR_WORK_MINS { get; set; }

        [JsonPropertyName("OPR_WORK_HRS")]
        public string OPR_WORK_HRS { get; set; }

        [JsonPropertyName("ENTERED_BY")]
        public string ENTERED_BY { get; set; }

        [JsonPropertyName("ENTERED_BY_NAME")]
        public string ENTERED_BY_NAME { get; set; }

        [JsonPropertyName("WORKCENTER_DESC")]
        public string WORKCENTER_DESC { get; set; }

        [JsonPropertyName("POSTING_DATE")]
        public string POSTING_DATE { get; set; }

        [JsonPropertyName("UOM")]
        public string UOM { get; set; }

        [JsonPropertyName("UOM_QTY")]
        public string UOM_QTY { get; set; }

        [JsonPropertyName("MRP_CNTRL")]
        public string MRP_CNTRL { get; set; }

        [JsonPropertyName("PRD_SCH")]
        public string PRD_SCH { get; set; }

        [JsonPropertyName("MACH_IDEAL_TIME")]
        public string MACH_IDEAL_TIME { get; set; }

        [JsonPropertyName("TOT_CONF_QTY")]
        public string TOT_CONF_QTY { get; set; }
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
        public string NOTIF_STATUS { get; set; } = "";
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

    public class LossOrderSapRequest
    {
        [JsonPropertyName("ORDER")]
        public string ORDER { get; set; }

        [JsonPropertyName("NAV_LOSS")]
        public LossOrderContainer NAV_LOSS { get; set; }
    }

    public class LossOrderContainer
    {
        [JsonPropertyName("results")]
        public List<LossOrderItem> Results { get; set; }
    }

    public class LossOrderItem
    {
        [JsonPropertyName("ORDER")]
        public string ORDER { get; set; }

        [JsonPropertyName("OPR_NUM")]
        public string OPR_NUM { get; set; }

        [JsonPropertyName("SETUP_IDEAL_TIME")]
        public string SETUP_IDEAL_TIME { get; set; }

        [JsonPropertyName("MACH_IDEAL_TIME")]
        public string MACH_IDEAL_TIME { get; set; }

        [JsonPropertyName("WORKCENTER")]
        public string WORKCENTER { get; set; }
    }

    public class CombinedConfirmationRequest
    {
        public ProductionOrderConfirmationRequest ProductionOrder { get; set; }
        public LossOrderSapRequest LossOrder { get; set; }
    }

}
