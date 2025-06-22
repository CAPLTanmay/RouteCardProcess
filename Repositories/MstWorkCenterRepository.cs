using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class MstWorkCenterRepository : IMstWorkCenterRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IUserMessageService _userMessageService;
        private readonly ISystemLoggerRepository _systemLogger;

        public MstWorkCenterRepository(SqlConnectionFactory connectionFactory, IUserMessageService userMessageService, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _userMessageService = userMessageService;
            _systemLogger = systemLogger;
        }

        public async Task<int> AddMstWorkCenterAsync(MstWorkCenterRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Plant,
                    request.WorkCenter,
                    request.WorkCenterDesc,
                    request.Dept
                };

                return await connection.ExecuteAsync(
                    "usp_AddWorkCenter",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MstWorkCenterRepository", "AddMstWorkCenterAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<int> UpdateMstWorkCenterAsync(MstWorkCenterRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var parameters = new
                {
                    request.Plant,
                    request.WorkCenter,
                    request.WorkCenterDesc,
                    request.Dept
                };

                return await connection.ExecuteAsync(
                    "usp_UpdateWorkCenter",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MstWorkCenterRepository", "UpdateMstWorkCenterAsync", ex.ToString());
                return 0;
            }
        }

        public async Task<IEnumerable<MstWorkCenterRequest>> GetAllMstWorkCentersAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.QueryAsync<MstWorkCenterRequest>(
                    "usp_GetAllMstWorkCenters",
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MstWorkCenterRepository", "GetAllMstWorkCentersAsync", ex.ToString());
                return Enumerable.Empty<MstWorkCenterRequest>();
            }
        }

        public async Task<IEnumerable<string>> GetDistinctDepartmentsAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.QueryAsync<string>(
                    "usp_GetDistinctDepartments",
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MstWorkCenterRepository", "GetDistinctDepartmentsAsync", ex.ToString());
                return Enumerable.Empty<string>();
            }
        }

        public async Task<IEnumerable<MstWorkCenterRequest>> GetWorkCentersByDeptAsync(string dept)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                return await connection.QueryAsync<MstWorkCenterRequest>(
                    "usp_GetWorkCentersByDept",
                    new { Dept = dept },
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MstWorkCenterRepository", "GetWorkCentersByDeptAsync", ex.ToString());
                return Enumerable.Empty<MstWorkCenterRequest>();
            }
        }
        public async Task<int> DeleteMstWorkCenterAsync(string plant, string workCenter)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE MstWorkCenter 
                    SET IsActive = 0 
                    WHERE Plant = @Plant AND WorkCenter = @WorkCenter";

                return await connection.ExecuteAsync(query, new { Plant = plant, WorkCenter = workCenter });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("MstWorkCenterRepository", "DeleteMstWorkCenterAsync", ex.ToString());
                return 0;
            }
        }
    }
}
