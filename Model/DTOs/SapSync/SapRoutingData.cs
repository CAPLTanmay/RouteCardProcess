namespace RouteCardProcess.Model.DTOs.SapSync
{
    public class SapRoutingData
    {
        public string ORDER_NUMBER { get; set; }
        public string TARGET_QUANTITY { get; set; }
        public string CONFIRMED_QUANTIT { get; set; }
        public string OPERATION_NUMBER { get; set; }
        public string DESCRIPTION { get; set; }
        public string WORK_CENTER { get; set; }
        public string WORK_CENTER_TEXT { get; set; }
        public string SETUP_TIME { get; set; }
        public string PROCESSING_TIME { get; set; }
        public string STATUS { get; set; }
        public string SETUP_UNIT { get; set; }
        public string PROCESSING_UNIT { get; set; }
        public string MATERIAL { get; set; }
        public string MATERIAL_TEXT { get; set; }
        public string ORDER_TYPE { get; set; }
        public string PRODUCTION_PLANT { get; set; }
        public string UNIT { get; set; }
        public string MRP_CONTROLLER { get; set; }
        public string PRODUCTION_SCHEDULER { get; set; }
        public string CONTROL_KEY {  get; set; }

    }

    public class SapRoutingResponse
    {
        public SapRoutingResults d { get; set; }
    }

    public class SapRoutingResults
    {
        public List<SapRoutingData> results { get; set; }
    }

    public class RoutingDataResponse
    {
        public string WorkOrder { get; set; }
        public string WorkCenter { get; set; }
        public string OperationNo { get; set; }
        public int TotalQty { get; set; }
        public int S_ConfirmedQuantity { get; set; }
        public int? L_CompletedQty { get; set; }
        public int? PendingQty { get; set; }
        public TimeSpan? Std_SetupTime { get; set; }
        public TimeSpan? Std_Machining_Time { get; set; }
        public string OperationDescription { get; set; }
        public string WorkCenterText { get; set; }
        public string Material { get; set; }
        public string MaterialText { get; set; }
        public string? MaterialTextLink { get; set; }
        public string OrderTypeDesc { get; set; }
        public string ControlKey { get; set; }
    }
}
