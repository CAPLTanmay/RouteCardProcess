using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class StdExceptionRepository : IStdExceptionRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IUserMessageService _userMessageService;
        private readonly ISystemLoggerRepository _systemLogger;

        public StdExceptionRepository(SqlConnectionFactory connectionFactory, IUserMessageService userMessageService, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _userMessageService = userMessageService;
            _systemLogger = systemLogger;
        }

        public async Task<int> AddStdExceptionAsync(StdExceptionRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Reason_Code,
                    request.Comments_Std
                };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_AddStdException",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("StdExceptionRepository", "AddStdExceptionAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<int> UpdateStdExceptionAsync(StdExceptionRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Reason_Code,
                    request.Comments_Std
                };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_UpdateStdException",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("StdExceptionRepository", "UpdateStdExceptionAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<IEnumerable<StdExceptionRequest>> GetAllStdExceptionsAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var result = await connection.QueryAsync<StdExceptionRequest>(
                    "usp_GetAllStdExceptions",
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("StdExceptionRepository", "GetAllStdExceptionsAsync", ex.ToString());
                return Enumerable.Empty<StdExceptionRequest>();
            }
        }
    }
}
