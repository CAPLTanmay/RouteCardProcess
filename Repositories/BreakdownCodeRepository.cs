using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class BreakdownCodeRepository : IBreakdownCodeRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ISystemLoggerRepository _systemLogger;

        public BreakdownCodeRepository(SqlConnectionFactory connectionFactory, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _systemLogger = systemLogger;
        }
        public async Task<int> AddAsync(BreakdownCodeRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.BreakdownCodeGroup,
                    request.BreakdownCode,
                    request.BreakdownDescription,
                    IsActive = request.IsActive ?? true
                };

                return await connection.ExecuteAsync("usp_AddBreakdownCode", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakdownCodeRepository", "AddAsync", ex.ToString());
                return 0;
            }
        }
        public async Task<int> UpdateAsync(BreakdownCodeRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.BreakdownCodeGroup,
                    request.BreakdownCode,
                    request.BreakdownDescription,
                    IsActive = request.IsActive
                };

                return await connection.ExecuteAsync("usp_UpdateBreakdownCode", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakdownCodeRepository", "UpdateAsync", ex.ToString());
                return 0;
            }
        }
        public async Task<IEnumerable<BreakdownCodeRequest>> GetAllAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.QueryAsync<BreakdownCodeRequest>("usp_GetAllBreakdownCodes", commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakdownCodeRepository", "GetAllAsync", ex.ToString());
                return Enumerable.Empty<BreakdownCodeRequest>();
            }
        }
        public async Task<int> DeleteAsync(string breakdownCodeGroup, string breakdownCode)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new { BreakdownCodeGroup = breakdownCodeGroup, BreakdownCode = breakdownCode };

                return await connection.ExecuteAsync("usp_DeleteBreakdownCode", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakdownCodeRepository", "DeleteAsync", ex.ToString());
                return 0;
            }
        }
    }
}
