using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RouteCardProcess.Repositories
{
    public class BreakDownRepository
    {
        private readonly IConfiguration _config;

        public BreakDownRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        }

        public async Task<bool> StartBreakDownAsync(string workCenterNo, string operatorId, string? breakDownReasonCode = null)
        {
            using var connection = CreateConnection();
            var parameters = new
            {
                WorkCenterNo = workCenterNo,
                OperatorId = operatorId,
                BreakDownReasonCode = breakDownReasonCode
            };
            var rows = await connection.ExecuteAsync("sp_StartBreakDown", parameters, commandType: CommandType.StoredProcedure);
            return rows > 0;
        }
        public async Task<bool> EndBreakDownAsync(string workCenterNo, string? operatorId = null, string? breakDownReasonCode = null)
        {
            using var connection = CreateConnection();
            var parameters = new
            {
                WorkCenterNo = workCenterNo,
                OperatorId = operatorId,
                BreakDownReasonCode = breakDownReasonCode
            };
            var rows = await connection.ExecuteAsync("sp_EndBreakDown", parameters, commandType: CommandType.StoredProcedure);
            return rows > 0;
        }

    }
}
