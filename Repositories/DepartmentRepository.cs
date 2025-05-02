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

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        }

        public async Task<IEnumerable<DepartmentMaster>> GetAllAsync()
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                var departments = await connection.QueryAsync<DepartmentMaster>(
                    "usp_Department_GetAll",
                    commandType: System.Data.CommandType.StoredProcedure
                );
                return departments;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching departments.", ex);
            }
        }

        public async Task<int> AddAsync(DepartmentMaster department)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                var result = await connection.ExecuteAsync(
                    "usp_Department_Add",
                    new { department.DepartmentName },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error inserting department.", ex);
            }
        }
    }
}
