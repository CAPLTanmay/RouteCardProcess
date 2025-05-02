using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Model;
using System.Data;

namespace RouteCardProcess.Repositories
{
    public class LogInRepository
    {
        private readonly IConfiguration _config;

        public LogInRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        }

        public async Task<IEnumerable<LogInMaster>> GetAllAsync()
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                var result = await connection.QueryAsync<LogInMaster>(
                    "sp_GetAllLogins",
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching login records.", ex);
            }
        }

        public async Task<int> AddAsync(LogInMaster login)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                var result = await connection.ExecuteAsync(
                    "sp_AddLogin",
                    new
                    {
                        login.OperatorId,
                        login.OperatorName,
                        login.Password,
                        login.Role,
                        login.DepartmentId
                    },
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error inserting login record.", ex);
            }
        }

        public async Task<LogInMaster?> ValidateLoginAsync(string operatorId, string password)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                var user = await connection.QueryFirstOrDefaultAsync<LogInMaster>(
                    "sp_ValidateLogin",
                    new { OperatorId = operatorId, Password = password },
                    commandType: CommandType.StoredProcedure
                );
                return user;
            }
            catch (Exception ex)
            {
                throw new Exception("Error validating login credentials.", ex);
            }
        }

        public async Task<string> TryLogoutAsync(string setUpId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                var setup = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_TryLogout",
                    new { SetUpID = setUpId },
                    commandType: CommandType.StoredProcedure
                );
                if (setup == null)
                    return "Invalid Setup ID";
                if (setup.SetupStatus == "Setup Started")
                    return "Cannot logout. Setup is still in progress.";
                return "OK";
            }
            catch (Exception ex)
            {
                throw new Exception("Error during logout process.", ex);
            }
        }
    }
}
