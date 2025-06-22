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

        public LogInRepository(SqlConnectionFactory connectionFactory, ISetUpTransRepository setUpTransRepository, IKblAuthService kblService, KblAuthConfig authConfig, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _connectionFactory = connectionFactory;
            _setUpTransRepository = setUpTransRepository;
            _kblService = kblService;
            _authConfig = authConfig;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
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


        public async Task<(int Flag, string Message)> TryLogoutAsync(string workCenterNo, string workOrderNo, string operationNo)
        {
            try
            {
                var (flag, setupStatus, machiningStatus, message, _, _) = await _setUpTransRepository
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
