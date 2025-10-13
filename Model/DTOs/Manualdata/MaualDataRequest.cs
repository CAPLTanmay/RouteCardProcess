namespace RouteCardProcess.Model.DTOs.Manualdata
{
    public class MaualDataRequest
    {
        public string ProductionOrderNumber { get; set; }
    }

    public class GetMaualDataRequest
    {
        public string ProductionOrderNumber { get; set; }
        public string? DepartmentName { get; set; }
        public string? WorkCenter { get; set; }
    }
}
