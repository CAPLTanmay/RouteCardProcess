using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class PauseCodeRepository : IPauseCodeRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ISystemLoggerRepository _systemLogger;

        public PauseCodeRepository(SqlConnectionFactory connectionFactory, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _systemLogger = systemLogger;
        }

        public async Task<int> AddPauseCodeAsync(PauseCodeRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Plant,
                    request.PauseCode,
                    request.PauseCodeDesc,
                    request.PauseCodeDescM,
                    IsActive = request.IsActive ?? true
                };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_AddPauseCode",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("PauseCodeRepository", "AddPauseCodeAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<int> UpdatePauseCodeAsync(PauseCodeRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Plant,
                    request.PauseCode,
                    request.PauseCodeDesc,
                    request.PauseCodeDescM,
                    IsActive = request.IsActive
                };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_UpdatePauseCode",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("PauseCodeRepository", "UpdatePauseCodeAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<IEnumerable<PauseCodeRequest>> GetAllPauseCodesAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var result = await connection.QueryAsync<PauseCodeRequest>(
                    "usp_GetAllPauseCodes",
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("PauseCodeRepository", "GetAllPauseCodesAsync", ex.ToString());
                return Enumerable.Empty<PauseCodeRequest>();
            }
        }

        public async Task<int> DeletePauseCodeAsync(string plant, string pauseCode)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new { Plant = plant, PauseCode = pauseCode };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_DeletePauseCode",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("PauseCodeRepository", "DeletePauseCodeAsync", ex.ToString());
                return 0;
            }
        }
    }
}
