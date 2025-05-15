using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RouteCardProcess.Repositories
{
    public class BreakDownRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public BreakDownRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private SqlConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection();
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
            try
            {
                var rows = await connection.ExecuteAsync("sp_StartBreakDown", parameters, commandType: CommandType.StoredProcedure);
                Console.WriteLine($"Rows affected: {rows}");
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
                throw;
            }
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
