using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Employee;
using RouteCardProcess.Model.DTOs.RBACEmployee;
using RouteCardProcess.Model.Entities;


namespace RouteCardProcess.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IUserMessageService _userMessageService;
        private readonly IPasswordSecurityService _passwordService;
        private readonly ISystemLoggerRepository _systemLogger;

        public EmployeeRepository(SqlConnectionFactory connectionFactory, IUserMessageService userMessageService,IPasswordSecurityService passwordSecurityService, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _userMessageService = userMessageService;
            _systemLogger = systemLogger;
            _passwordService = passwordSecurityService;
        }
        private SqlConnection CreateConnection() => _connectionFactory.CreateConnection();
        public async Task<string> AddEmployeeAsync(EmployeeRequest request)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                string plainPassword = request.EmployeePassword ?? string.Empty;

                string encryptedPassword = request.IsContractEmployee
                    ? await _passwordService.EncryptPassword(plainPassword)
                    : plainPassword;

                var parameters = new DynamicParameters();
                parameters.Add("EmployeeId", request.EmployeeId);
                parameters.Add("EmployeeCode", request.EmployeeCode);
                parameters.Add("FirstName", request.FirstName);
                parameters.Add("LastName", request.LastName);
                parameters.Add("UserRole", request.UserRole);
                parameters.Add("UserDepartment", request.UserDepartment);
                parameters.Add("IsContractEmployee", request.IsContractEmployee);
                parameters.Add("EmployeePassword", encryptedPassword);
                parameters.Add("EmployeeStartDate", request.EmployeeStartDate);
                parameters.Add("EmployeeEndDate", request.EmployeeEndDate);
                parameters.Add("IsTempPassword", request.IsTempPassword);
                parameters.Add("CreatedBy", request.CreatedBy);
                parameters.Add("CreatedOn", DateTime.Now);
                parameters.Add("Result", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

                await connection.ExecuteAsync("usp_AddEmployee", parameters, transaction, commandType: CommandType.StoredProcedure);
                string result = parameters.Get<string>("Result");

                if (request.DepartmentIds?.Any() == true)
                {
                    var userDepartments = request.DepartmentIds
                        .Select(d => new UserDepartmentMapping { UserId = request.EmployeeId, DepartmentId = d.DepartmentId,DepartmentName=d.DepartmentName })
                        .ToList();

                    await AddNewUserDepartments(userDepartments, connection, transaction);
                }

                transaction.Commit();
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                await _systemLogger.LogAsync("EmployeeRepository", "AddEmployeeAsync", ex.ToString());
                return "0";
            }
        }

        public async Task<string> UpdateEmployeeAsync(UpdateEmployeeRequest request)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("EmployeeId", request.EmployeeId);
                parameters.Add("EmployeeCode", request.EmployeeCode);
                parameters.Add("FirstName", request.FirstName);
                parameters.Add("LastName", request.LastName);
                parameters.Add("UserRole", request.UserRole);
                parameters.Add("UserDepartment", request.UserDepartment);
                parameters.Add("IsContractEmployee", request.IsContractEmployee);
                parameters.Add("EmployeePassword", request.EmployeePassword);
                parameters.Add("EmployeeStartDate", request.EmployeeStartDate);
                parameters.Add("EmployeeEndDate", request.EmployeeEndDate);
                parameters.Add("IsTempPassword", request.IsTempPassword);
                parameters.Add("UpdatedBy", request.UpdatedBy);
                parameters.Add("UpdatedOn", DateTime.Now);
                parameters.Add("Result", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

                await connection.ExecuteAsync("usp_UpdateEmployee", parameters, transaction, commandType: CommandType.StoredProcedure);
                string result = parameters.Get<string>("Result");

                var deleteParams = new DynamicParameters();
                deleteParams.Add("UserId", request.EmployeeId);

                await connection.ExecuteAsync("usp_DeleteUserDepartmentsByUserId", deleteParams, transaction, commandType: CommandType.StoredProcedure);


                if (request.DepartmentIds?.Any() == true)
                {
                    var userDepartments = request.DepartmentIds
                        .Select(d => new UserDepartmentMapping { UserId = request.EmployeeId, DepartmentId = d.DepartmentId,DepartmentName = d.DepartmentName })
                        .ToList();

                    await AddNewUserDepartments(userDepartments, connection, transaction);
                }

                transaction.Commit();
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                await _systemLogger.LogAsync("EmployeeRepository", "UpdateEmployeeAsync", ex.ToString());
                return "0";
            }
        }
        public async Task<IEnumerable<EmployeeResponse>> GetAllEmployeesAsync()
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            var employeeDictionary = new Dictionary<int, EmployeeResponse>();

            try
            {
                var result = await connection.QueryAsync<EmployeeResponse, DepartmentDto, EmployeeResponse>(
                    "usp_GetAllEmployees",
                    (employee, department) =>
                    {
                        if (!employeeDictionary.TryGetValue(employee.EmployeeId, out var empEntry))
                        {
                            empEntry = employee;
                            empEntry.Departments = new List<DepartmentDto>();
                            employeeDictionary.Add(empEntry.EmployeeId, empEntry);
                        }

                        if (department != null && department.DepartmentId > 0)
                            empEntry.Departments.Add(department);

                        return empEntry;
                    },
                    splitOn: "DepartmentId",
                    commandType: CommandType.StoredProcedure
                );

                return employeeDictionary.Values;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("EmployeeRepository", "GetAllEmployeesAsync", ex.ToString());
                return Enumerable.Empty<EmployeeResponse>();
            }
        }
        public async Task<EmployeeResponse?> GetEmployeeByIdAsync(GetEmployeeRequest request)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            var employeeDictionary = new Dictionary<int, EmployeeResponse>();

            try
            {
                var result = await connection.QueryAsync<EmployeeResponse, DepartmentDto, EmployeeResponse>(
                    "usp_GetEmployeeById",
                    (employee, department) =>
                    {
                        if (!employeeDictionary.TryGetValue(employee.EmployeeId, out var empEntry))
                        {
                            empEntry = employee;
                            empEntry.Departments = new List<DepartmentDto>();
                            employeeDictionary.Add(empEntry.EmployeeId, empEntry);
                        }

                        if (department != null && department.DepartmentId > 0)
                            empEntry.Departments.Add(department);

                        return empEntry;
                    },
                    new { EmployeeId = request.EmployeeId },
                    splitOn: "DepartmentId",
                    commandType: CommandType.StoredProcedure
                );

                return employeeDictionary.Values.FirstOrDefault();
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("EmployeeRepository", "GetEmployeeByIdAsync", ex.ToString());
                return null;
            }
        }
        public async Task<string> SoftDeleteEmployeeAsync(int employeeCode, int updatedBy)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("EmployeeCode", employeeCode);
            parameters.Add("UpdatedBy", updatedBy);
            parameters.Add("UpdatedOn", DateTime.Now);

            await connection.ExecuteAsync("usp_SoftDeleteEmployee", parameters, commandType: CommandType.StoredProcedure);
            return _userMessageService.GetMessage(1113);
        }
        public async Task AddNewUserDepartments(List<UserDepartmentMapping> departments, IDbConnection connection, IDbTransaction transaction)
        {
            foreach (var dept in departments)
            {
                var parameters = new DynamicParameters();
                parameters.Add("UserId", dept.UserId);
                parameters.Add("DepartmentId", dept.DepartmentId);
                parameters.Add("DepartmentName", dept.DepartmentName);

                await connection.ExecuteAsync("usp_AddUserDepartments", parameters, transaction, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<bool> ResetTempPasswordAsync(ResetPasswordDto dto)
        {
            using var connection = _connectionFactory.CreateConnection();
            var employee = await connection.QueryFirstOrDefaultAsync<MSTEmployee>("usp_GetTempEmployeeForReset",new { dto.OperatorId },commandType: CommandType.StoredProcedure);

            if (employee == null)
                return false;

            var decrypted = await _passwordService.DecryptPassword(employee.EmployeePassword);
            if (decrypted != dto.TempPassword)
                return false;

            var encryptedNew = await _passwordService.EncryptPassword(dto.NewPassword);
            var result = await connection.QueryFirstOrDefaultAsync<int>("usp_ResetTempPassword",new { OperatorId = dto.OperatorId, NewPass = encryptedNew },commandType: CommandType.StoredProcedure);

            return result > 0;
        }
    }
}
