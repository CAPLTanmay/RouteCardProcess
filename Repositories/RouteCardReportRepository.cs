using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;

namespace RouteCardProcess.Repositories
{
    public class RouteCardReportRepository:IRouteCardReportRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ILogInRepository _logInRepository;

        public RouteCardReportRepository(SqlConnectionFactory connectionFactory, ILogInRepository logInRepository)
        {
            _connectionFactory = connectionFactory;
            _logInRepository = logInRepository;
        }

        public async Task<IEnumerable<RouteCardReportModel>> GetRouteCardReportAsync(string workOrderNo)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var result = (await connection.QueryAsync<RouteCardReportModel>(
                "sp_GetRouteCardReport",
                new { WorkOrderNo = workOrderNo },
                commandType: CommandType.StoredProcedure)).AsList();

            foreach (var item in result)
            {
                DateTime shiftTime;
                if (!string.IsNullOrWhiteSpace(item.MachiningEndTime) &&
                    DateTime.TryParse(item.MachiningEndTime, out shiftTime))
                {
                    item.Shift = _logInRepository.GetCurrentShift(shiftTime);
                }
                else
                {
                    item.Shift = _logInRepository.GetCurrentShift(DateTime.Now);
                }
            }

            return result;
        }
    }
}
