using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class LossOrderRepository : ILossOrderRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ISystemLoggerRepository _systemLogger;

        public LossOrderRepository(SqlConnectionFactory connectionFactory, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _systemLogger = systemLogger;
        }

        public async Task<int> AddLossOrderAsync(LossOrderRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.LossOrderDepartment,
                    request.LossOrderYearMonth,
                    request.LossOrderNumber,
                    request.LossOrderDesc,
                    IsActive = request.IsActive ?? true
                };

                return await connection.ExecuteAsync("usp_AddLossOrder", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (ex.Message.Contains("already exists"))
            {
                throw new ApplicationException("LossOrder already exists.");
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LossOrderRepository", "AddLossOrderAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<int> UpdateLossOrderAsync(LossOrderRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.LossOrderDepartment,
                    request.LossOrderYearMonth,
                    request.LossOrderNumber,
                    request.LossOrderDesc,
                    request.IsActive
                };

                return await connection.ExecuteAsync("usp_UpdateLossOrder", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LossOrderRepository", "UpdateLossOrderAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<IEnumerable<LossOrderRequest>> GetAllLossOrdersAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.QueryAsync<LossOrderRequest>(
                    "usp_GetAllLossOrders",
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LossOrderRepository", "GetAllLossOrdersAsync", ex.ToString());
                return Enumerable.Empty<LossOrderRequest>();
            }
        }

        public async Task<int> DeleteLossOrderAsync(DeleteLossOrderRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new { request.LossOrderDepartment, request.LossOrderYearMonth, request.LossOrderNumber };

                return await connection.ExecuteAsync(
                    "usp_DeleteLossOrder",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LossOrderRepository", "DeleteLossOrderAsync", ex.ToString());
                return 0;
            }
        }
    }
}
