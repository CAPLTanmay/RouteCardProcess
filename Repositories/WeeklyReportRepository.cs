using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.WeeklyReport;

namespace RouteCardProcess.Repositories
{
    public class WeeklyReportRepository : IWeeklyReportRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ILogInRepository _logInRepository;

        public WeeklyReportRepository(SqlConnectionFactory connectionFactory, ILogInRepository logInRepository)
        {
            _connectionFactory = connectionFactory;
            _logInRepository = logInRepository;
        }

        public async Task<IEnumerable<WeeklyExceptionReportModel>> GetExceptionReportAsync(WeeklyReportRequestDto request)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var result = (await connection.QueryAsync<WeeklyExceptionReportModel>(
            "usp_GetWeeklyMachiningExceptionReport",
            new
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                DepartmentId = string.IsNullOrEmpty(request.Department) ? (int?)null : int.Parse(request.Department)
            },
            commandType: CommandType.StoredProcedure)).AsList();
            return result;
        }
    }
}
