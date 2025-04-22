
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Model;

namespace RouteCardProcess.Repositories
{
    public class DepartmentRepository
    {
        private readonly IConfiguration _config;

        public DepartmentRepository(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IEnumerable<DepartmentMaster>> GetAllAsync()
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var sql = "SELECT DepartmentId, DepartmentName FROM DepartmentMaster";
            return await connection.QueryAsync<DepartmentMaster>(sql);
        }

        public async Task<int> AddAsync(DepartmentMaster department)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var sql = "INSERT INTO DepartmentMaster (DepartmentName) VALUES (@DepartmentName)";
            return await connection.ExecuteAsync(sql, department);
        }
    }
}
