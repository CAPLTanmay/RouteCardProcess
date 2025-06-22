using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class ExceptionReasonRepository : IExceptionReasonRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IUserMessageService _userMessageService;
        private readonly ISystemLoggerRepository _systemLogger;

        public ExceptionReasonRepository(SqlConnectionFactory connectionFactory, IUserMessageService userMessageService, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _userMessageService = userMessageService;
            _systemLogger = systemLogger;
        }

        public async Task<int> AddExceptionReasonAsync(ExceptionReasonRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Reason_Code,
                    request.Reason_desc,
                    request.Reason_descM,
                    request.Comments_Std,
                    IsActive = request.IsActive ?? true
                };

                return await connection.ExecuteAsync("usp_AddExceptionReason", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ExceptionReasonRepository", "AddExceptionReasonAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<int> UpdateExceptionReasonAsync(ExceptionReasonRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Reason_Code,
                    request.Reason_desc,
                    request.Reason_descM,
                    request.Comments_Std,
                    request.IsActive
                };

                return await connection.ExecuteAsync("usp_UpdateExceptionReason", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ExceptionReasonRepository", "UpdateExceptionReasonAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<IEnumerable<ExceptionReasonRequest>> GetAllExceptionReasonsAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.QueryAsync<ExceptionReasonRequest>(
                    "usp_GetAllExceptionReasons",
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ExceptionReasonRepository", "GetAllExceptionReasonsAsync", ex.ToString());
                return Enumerable.Empty<ExceptionReasonRequest>();
            }
        }

        public async Task<int> DeleteExceptionReasonAsync(string reasonCode) 
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new { Reason_Code = reasonCode };

                return await connection.ExecuteAsync("usp_DeleteExceptionReason", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ExceptionReasonRepository", "DeleteExceptionReasonAsync", ex.ToString());
                return 0;
            }
        }
    }
}
