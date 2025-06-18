using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Department;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IUserMessageService _userMessageService;
        public DepartmentRepository(SqlConnectionFactory connectionFactory, IUserMessageService userMessageService)
        {
            _connectionFactory = connectionFactory;
            _userMessageService = userMessageService;
        }

        private SqlConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection();
        }
        public async Task<IEnumerable<DepartmentMaster>> GetAllAsync()
        {
            try
            {
                using var connection = CreateConnection();
                var departments = await connection.QueryAsync<DepartmentMaster>(
                    "usp_Department_GetAll",
                    commandType: System.Data.CommandType.StoredProcedure
                );
                return departments;
            }
            catch (Exception ex)
            {
                throw new Exception(_userMessageService.GetMessage(1017), ex);
            }
        }

        public async Task<int> AddAsync(DepartmentMasterDto department)
        {
            try
            {
                using var connection = CreateConnection();
                var rowsAffected = await connection.ExecuteAsync(
                    "usp_Department_Add",
                    new { department.DepartmentName },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                return rowsAffected;
            }
            catch (Exception ex)
            {
                throw new Exception(_userMessageService.GetMessage(1016), ex);
            }
        }
    }
}
