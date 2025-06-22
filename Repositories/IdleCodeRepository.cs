using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class IdleCodeRepository : IIdleCodeRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ISystemLoggerRepository _systemLogger;

        public IdleCodeRepository(SqlConnectionFactory connectionFactory, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _systemLogger = systemLogger;
        }

        public async Task<int> AddIdleCodeAsync(IdleCodeRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Plant,
                    request.IdleCode,
                    request.IdleCodeDesc,
                    request.IdleCodeDescM,
                    IsActive = request.IsActive ?? true
                };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_AddIdleCode",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("IdleCodeRepository", "AddIdleCodeAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<int> UpdateIdleCodeAsync(IdleCodeRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Plant,
                    request.IdleCode,
                    request.IdleCodeDesc,
                    request.IdleCodeDescM,
                    request.IsActive
                };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_UpdateIdleCode",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("IdleCodeRepository", "UpdateIdleCodeAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<IEnumerable<IdleCodeRequest>> GetAllIdleCodesAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var result = await connection.QueryAsync<IdleCodeRequest>(
                    "usp_GetAllIdleCodes",
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("IdleCodeRepository", "GetAllIdleCodesAsync", ex.ToString());
                return Enumerable.Empty<IdleCodeRequest>();
            }
        }
        public async Task<int> DeleteIdleCodeAsync(string plant, string idleCode)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    Plant = plant,
                    IdleCode = idleCode
                };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_DeleteIdleCode",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("IdleCodeRepository", "DeleteIdleCodeAsync", ex.ToString());
                return 0;
            }
        }

    }
}
