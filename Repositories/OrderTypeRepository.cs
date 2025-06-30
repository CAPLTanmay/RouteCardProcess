using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class OrderTypeRepository : IOrderTypeRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ISystemLoggerRepository _systemLogger;
       
        public OrderTypeRepository(SqlConnectionFactory connectionFactory, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _systemLogger = systemLogger;
        }

        public async Task<int> AddOrderTypeAsync(OrderTypeRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Plant,
                    request.OrderType,
                    request.OrderTypeDesc,
                    IsActive = request.IsActive ?? true
                };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_AddOrderType",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (SqlException ex) when (ex.Message.Contains("OrderType already exists"))
            {
                throw new ApplicationException("OrderType already exists.");
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("OrderTypeRepository", "AddOrderTypeAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<int> UpdateOrderTypeAsync(OrderTypeRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Plant,
                    request.OrderType,
                    request.OrderTypeDesc,
                    IsActive = request.IsActive
                };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_UpdateOrderType",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("OrderTypeRepository", "UpdateOrderTypeAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<IEnumerable<OrderTypeRequest>> GetAllOrderTypesAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var result = await connection.QueryAsync<OrderTypeRequest>(
                    "usp_GetAllOrderTypes",
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("OrderTypeRepository", "GetAllOrderTypesAsync", ex.ToString());
                return Enumerable.Empty<OrderTypeRequest>();
            }
        }

        public async Task<int> DeleteOrderTypeAsync(string plant, string orderType)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new { Plant = plant, OrderType = orderType };

                int rowsAffected = await connection.ExecuteAsync(
                    "usp_DeleteOrderType",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return rowsAffected;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("OrderTypeRepository", "DeleteOrderTypeAsync", ex.ToString());
                return 0;
            }
        }
    }
}
