using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Module;

namespace RouteCardProcess.Repositories
{
    public class ModuleRepository : IModuleRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ISystemLoggerRepository _logger;

        public ModuleRepository(SqlConnectionFactory connectionFactory, ISystemLoggerRepository logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<ModuleResponseDto>> GetAllModulesAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var result = await connection.QueryAsync<ModuleResponseDto>(
                    "USP_GetAllModules",
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync("ModuleRepository", "GetAllModulesAsync", ex.ToString());
                return Enumerable.Empty<ModuleResponseDto>();
            }
        }
    }
}
