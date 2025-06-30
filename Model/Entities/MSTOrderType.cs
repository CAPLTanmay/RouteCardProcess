namespace RouteCardProcess.Model.Entities
{
    public class OrderTypeRequest
    {
        public string Plant { get; set; }
        public string OrderType { get; set; }
        public string OrderTypeDesc { get; set; }
        public bool? IsActive { get; set; }
    }
    public class DeleteOrderTypeRequest
    {
        public string Plant { get; set; }
        public string OrderType { get; set; }
    }

}
