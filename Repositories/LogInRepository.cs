using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Model;
namespace RouteCardProcess.Repositories
{
    public class LogInRepository
    {
        private readonly IConfiguration _config;

        public LogInRepository(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IEnumerable<LogInMaster>> GetAllAsync()
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var sql = "SELECT TOP 1000 * FROM LogInMaster";
            return await connection.QueryAsync<LogInMaster>(sql);
        }

        public async Task<int> AddAsync(LogInMaster login)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var sql = @"
            INSERT INTO LogInMaster (OperatorId, OperatorName, Password, Role, DepartmentId)
            VALUES (@OperatorId, @OperatorName, @Password, @Role, @DepartmentId)";
            return await connection.ExecuteAsync(sql, login);
        }

        public async Task<LogInMaster?> ValidateLoginAsync(string operatorId, string password)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var sql = @"SELECT * FROM LogInMaster 
                WHERE OperatorId = @OperatorId AND Password = @Password";
            return await connection.QueryFirstOrDefaultAsync<LogInMaster>(sql, new { OperatorId = operatorId, Password = password });
        }

        public async Task<string> TryLogoutAsync(string setUpId)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            var setup = await connection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT SetupStatus FROM SetUp_Trans_Master WHERE SetUpID = @SetUpID",
                new { SetUpID = setUpId });

            if (setup == null)
                return "Invalid Setup ID";

            if (setup.SetupStatus == "Setup Started")
                return "Cannot logout. Setup is still in progress.";

            return "OK"; // Proceed with logout
        }

    }
}
