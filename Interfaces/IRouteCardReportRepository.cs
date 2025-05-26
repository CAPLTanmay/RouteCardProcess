namespace RouteCardProcess.Interfaces
{
    public interface IRouteCardReportRepository
    {
         Task<IEnumerable<RouteCardReportModel>> GetRouteCardReportAsync(string workOrderNo);

    }
}
