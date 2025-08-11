using System.Data;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Configurations;
using RouteCardProcess.Model.DTOs.Login;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class LogInRepository : ILogInRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ISetUpTransRepository _setUpTransRepository;
        private readonly IKblAuthService _kblService;
        private readonly KblAuthConfig _authConfig;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;
        private readonly IPasswordSecurityService _passwordService;

        public LogInRepository(SqlConnectionFactory connectionFactory, ISetUpTransRepository setUpTransRepository, IKblAuthService kblService, KblAuthConfig authConfig, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService, IPasswordSecurityService passwordService)
        {
            _connectionFactory = connectionFactory;
            _setUpTransRepository = setUpTransRepository;
            _kblService = kblService;
            _authConfig = authConfig;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
            _passwordService = passwordService;
        }

        public async Task<IEnumerable<LogInMaster>> GetAllAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                var result = await connection.QueryAsync<LogInMaster>(
                    "usp_GetAllLogins",
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LogInRepository", "GetAllAsync", ex.ToString());
                var msg = _userMessageService.GetMessage(5001); // Internal Server Error
                throw new Exception(msg, ex);
            }
        }

        public async Task<int> AddAsync(LogInMaster login)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                var result = await connection.ExecuteAsync(
                    "usp_AddLogin",
                    new
                    {
                        login.OperatorId,
                        login.OperatorName,
                        login.OperatorPassword,
                        login.OperatorRole,
                        login.DepartmentId,
                        login.OperatorDummyID
                    },
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LogInRepository", "AddLoginAsync", ex.ToString());
                var msg = _userMessageService.GetMessage(5001);
                throw new Exception(msg, ex);
            }
        }
        public async Task<LogInMaster?> ValidateLoginAsync(string operatorId, string password)
        {
            try
            {
                var useKblAuth = _authConfig.UseKblAuthAPI;

                if (useKblAuth)
                {
                    var encryptedPassword = await _kblService.EncryptPasswordAsync(password);

                    if (!string.IsNullOrEmpty(encryptedPassword))
                    {
                        var kblLogin = new KblLoginRequest
                        {
                            StrLoginId = operatorId,
                            StrPassword = encryptedPassword
                        };

                        var kblAuthResponse = await _kblService.AuthenticateLoginAsync(kblLogin);

                        if (kblAuthResponse == "success")
                        {
                            var token = await _kblService.GetTokenAsync();
                            var empInfoResponse = await _kblService.GetEmployeeInfoAsync(token, operatorId);
                            var emp = empInfoResponse?.EmpInfo?.FirstOrDefault();

                            if (emp != null)
                            {
                                return new LogInMaster
                                {
                                    OperatorId = emp.Tktno,
                                    OperatorName = emp.Name,
                                    OperatorRole = emp.Designation,
                                    DepartmentId = 3,
                                    DepartmentName = emp.Deptnm,
                                    Shift = await GetCurrentShiftAsync(),
                                    IsFromKBL = true
                                };
                            }
                        }
                    }
                }
                else
                {
                    // Not using KBL auth but still fetch KBL employee info
                    var token = await _kblService.GetTokenAsync();
                    var empInfoResponse = await _kblService.GetEmployeeInfoAsync(token, operatorId);
                    var emp = empInfoResponse?.EmpInfo?.FirstOrDefault();

                    if (emp != null)
                    {
                        return new LogInMaster
                        {
                            OperatorId = emp.Tktno,
                            OperatorName = emp.Name,
                            OperatorRole = emp.Designation,
                            DepartmentId = 3,
                            DepartmentName = emp.Deptnm,
                            Shift = await GetCurrentShiftAsync(),
                            IsFromKBL = true
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LogInRepository", "ValidateLogin", ex.ToString());
            }
            // Fallback: Validate from local database
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var user = await connection.QueryFirstOrDefaultAsync<LogInMaster>
            ("usp_ValidateLogin",
                 new { OperatorId = operatorId, OperatorPassword = password },
                 commandType: CommandType.StoredProcedure
            );

            if (user != null)
            {
                user.Shift = await GetCurrentShiftAsync();
                user.IsFromKBL = false;
            }

            return user;
        }

        public async Task<LoginResult> LoginEmployeeAsync(string operatorId, string password)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Step 0: Get employee details by EmployeeCode
                var employee = await connection.QueryFirstOrDefaultAsync<dynamic>("usp_GetEmployeeLoginInfo",new { OperatorId = operatorId },commandType: CommandType.StoredProcedure);

                if (employee == null)
                {
                    return new LoginResult
                    {
                        IsSuccess = false,
                        FailureReason = "Invalid operator ID. Not for DRC app"
                    };
                }

                // Fetch all mapped department names for the user
                var departmentNames = (await connection.QueryAsync<string>("usp_GetMappedDepartmentNames",new { UserId = employee.EmployeeId },commandType: CommandType.StoredProcedure)).ToList();

                bool isContract = employee.IsContractEmployee;

                if (isContract)
                {
                    bool isTempPassword = employee.IsTempPassword;

                    // Encrypt input password
                    var encryptedInputPassword = await _passwordService.EncryptPassword(password);
                    if (encryptedInputPassword != employee.EmployeePassword)
                    {
                        return new LoginResult
                        {
                            IsSuccess = false,
                            FailureReason = "Invalid password."
                        };
                    }

                    if (isTempPassword)
                    {
                        return new LoginResult
                        {
                            IsSuccess = true,
                            IsTempPassword = true,
                            User = new LoginUserDto
                            {
                                SrNo=employee.EmployeeId,
                                OperatorId = employee.EmployeeCode.ToString(),
                                ContractEmpId=employee.ContractEmpId.ToString(),
                                OperatorName = employee.FirstName + " " + employee.LastName,
                                OperatorRole = employee.UserRole,
                                DepartmentId = 0, // You can map department from mapping table
                                DepartmentName = employee.UserDepartment,
                                RBACDepartmentName = departmentNames,
                                Shift = await GetCurrentShiftAsync(),
                                IsFromKBL = false                        
                            }
                        };
                    }

                    // Success login
                    return new LoginResult
                    {
                        IsSuccess = true,
                        IsTempPassword = false,
                        User = new LoginUserDto
                        {
                            SrNo = employee.EmployeeId,
                            OperatorId = employee.EmployeeCode.ToString(),
                            ContractEmpId = employee.ContractEmpId.ToString(),
                            OperatorName = employee.FirstName + " " + employee.LastName,
                            OperatorRole = employee.UserRole,
                            DepartmentId = 0,
                            DepartmentName = employee.UserDepartment,
                            RBACDepartmentName = departmentNames,
                            Shift = await GetCurrentShiftAsync(),
                            IsFromKBL = false
                        }
                    };
                }

                // Non-contract employee → use KBL auth logic
                var useKblAuth = _authConfig.UseKblAuthAPI;
                if (useKblAuth)
                {
                    var encryptedPassword = await _kblService.EncryptPasswordAsync(password);
                    if (!string.IsNullOrEmpty(encryptedPassword))
                    {
                        var kblLogin = new KblLoginRequest
                        {
                            StrLoginId = operatorId,
                            StrPassword = encryptedPassword
                        };

                        var kblAuthResponse = await _kblService.AuthenticateLoginAsync(kblLogin);
                        if (kblAuthResponse == "success")
                        {
                            var token = await _kblService.GetTokenAsync();
                            var empInfoResponse = await _kblService.GetEmployeeInfoAsync(token, operatorId);
                            var emp = empInfoResponse?.EmpInfo?.FirstOrDefault();

                            if (emp != null)
                            {
                                return new LoginResult
                                {
                                    IsSuccess = true,
                                    User = new LoginUserDto
                                    {
                                        OperatorId = emp.Tktno,
                                        OperatorName = emp.Name,
                                        OperatorRole = emp.Designation,
                                        DepartmentId = 3,
                                        DepartmentName = emp.Deptnm,
                                        Shift = await GetCurrentShiftAsync(),
                                        IsFromKBL = true
                                    }
                                };
                            }
                        }
                    }

                    return new LoginResult
                    {
                        IsSuccess = false,
                        FailureReason = "KBL authentication failed."
                    };
                }
                else
                {
                    // fallback to non-contract fallback method
                    return new LoginResult
                    {
                        IsSuccess = true,
                        IsTempPassword = false,
                        User = new LoginUserDto
                        {
                            SrNo = employee.EmployeeId,
                            OperatorId = employee.EmployeeCode.ToString(),
                            ContractEmpId = employee.ContractEmpId.ToString(),
                            OperatorName = employee.FirstName + " " + employee.LastName,
                            OperatorRole = employee.UserRole,

                            DepartmentId = 0,
                            DepartmentName = employee.UserDepartment,
                            RBACDepartmentName = departmentNames,
                            Shift = await GetCurrentShiftAsync(),
                            IsFromKBL = true
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LogInRepository", "LoginEmployeeAsync", ex.ToString());
                return new LoginResult
                {
                    IsSuccess = false,
                    FailureReason = "Internal server error"
                };
            }
        }


        public async Task<(int Flag, string Message)> TryLogoutAsync(string workCenterNo, string workOrderNo, string operationNo)
        {
            try
            {
                var (flag, setupStatus, machiningStatus, message, _, _,_) = await _setUpTransRepository
                    .CheckSetupNotificationStatusAsync(workCenterNo, workOrderNo, operationNo);

                if (flag == 0)
                    return (0, message);
                if(setupStatus == "Setup Started" || machiningStatus == "Machining Started")
                    return (0, _userMessageService.GetMessage(1004)); 

                return (1, _userMessageService.GetMessage(1005));
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LogInRepository", "TryLogoutAsync", ex.ToString());
                return (0, _userMessageService.GetMessage(5001));
            }
        }

        public async Task<string> GetCurrentShiftAsync(DateTime? dateTime = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();


            // Determine the time to be evaluated
            var timeNow = (dateTime ?? DateTime.Now).TimeOfDay;

            // Define parameters for stored procedure execution
            var parameters = new { TimeNow = timeNow };

            // Execute the stored procedure to retrieve the current shift code
            var shift = await connection.QueryFirstOrDefaultAsync<string>(
                "usp_GetCurrentShift",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // Return the result or a fallback value
            return shift ?? _userMessageService.GetMessage(1072);
        }


    }
}
