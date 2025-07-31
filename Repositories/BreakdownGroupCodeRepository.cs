using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class BreakdownGroupCodeRepository : IBreakdownGroupCodeRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ISystemLoggerRepository _systemLogger;
        public BreakdownGroupCodeRepository(SqlConnectionFactory connectionFactory, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _systemLogger = systemLogger;
        }

        public async Task<int> AddAsync(BreakdownGroupCodeRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                var parameters = new
                {
                    request.BreakdownCodeGroup,
                    request.GroupDescription,
                    IsActive = request.IsActive ?? true
                };
                return await connection.ExecuteAsync("usp_AddBreakdownGroupCode", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakdownGroupCodeRepository", "AddAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<int> UpdateAsync(BreakdownGroupCodeRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.BreakdownCodeGroup,
                    request.GroupDescription,
                    IsActive = request.IsActive
                };
                return await connection.ExecuteAsync("usp_UpdateBreakdownGroupCode", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakdownGroupCodeRepository", "UpdateAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<IEnumerable<BreakdownGroupCodeRequest>> GetAllAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.QueryAsync<BreakdownGroupCodeRequest>(
                    "usp_GetAllBreakdownGroupCodes",
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakdownGroupCodeRepository", "GetAllAsync", ex.ToString());
                return Enumerable.Empty<BreakdownGroupCodeRequest>();
            }
        }
        public async Task<int> DeleteAsync(string breakdownCodeGroup)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new { BreakdownCodeGroup = breakdownCodeGroup };

                return await connection.ExecuteAsync("usp_DeleteBreakdownGroupCode", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakdownGroupCodeRepository", "DeleteAsync", ex.ToString());
                return 0;
            }
        }
        public async Task<IEnumerable<BreakdownCodeRequest>> GetByGroupAsync(string breakdownCodesByGroup)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new { BreakdownCodeGroup = breakdownCodesByGroup };

                var result = await connection.QueryAsync<BreakdownCodeRequest>(
                    "usp_GetBreakdownCodesByGroup",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakdownCodeRepository", "GetByGroupAsync", ex.ToString());
                return Enumerable.Empty<BreakdownCodeRequest>();
            }
        }

    }
}
