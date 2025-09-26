namespace RouteCardProcess.Model.DTOs.SapValidation
{
    public class WorkCenterRequest
    {
        public string WorkCenter { get; set; }
    }

    public class ValidateOrderRequest
    {
        public string Order { get; set; }
        public string WorkCenter { get; set; }
    }

    public class RoutingDataRequest
    {
        public string OrderNumber { get; set; }
    }
    public class MaterialTextLinkRequest
    {
        public string material { get; set; }
    }
}
