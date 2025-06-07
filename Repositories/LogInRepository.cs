using System.Data;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Dapper;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model;
using RouteCardProcess.Model.DTOs.Login;
using RouteCardProcess.Model.Entities;
using RouteCardProcess.Model.Configurations;
using Microsoft.Extensions.Configuration;

namespace RouteCardProcess.Repositories
{
    public class LogInRepository : ILogInRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ISetUpTransRepository _setUpTransRepository;
        private readonly IKblAuthService _kblService;
        private readonly IConfiguration _configuration;
        private readonly KblAuthConfig _authConfig;
        private readonly HttpClient _httpClient;
        private readonly ILogger<LogInRepository> _logger;
        private readonly ISystemLoggerRepository _systemLogger;

        public LogInRepository(SqlConnectionFactory connectionFactory, ISetUpTransRepository setUpTransRepository, IKblAuthService kblService, KblAuthConfig authConfig,IConfiguration configuration, ILogger<LogInRepository> logger, ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _setUpTransRepository = setUpTransRepository;
            _kblService = kblService;
            _authConfig = authConfig;
            _configuration = configuration;
            _logger = logger;
            _systemLogger = systemLogger;
        }

        public async Task<IEnumerable<LogInMaster>> GetAllAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
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
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                var result = await connection.ExecuteAsync(
                    "sp_AddLogin",
                    new
                    {
                        login.OperatorId,
                        login.OperatorName,
                        login.OperatorPassword,
                        login.OperatorRole,
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
                                    Shift = GetCurrentShift(),
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
                            Shift = GetCurrentShift(),
                            IsFromKBL = true
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString(), "KBL login failed for operator ID: {OperatorId}", operatorId);
                await _systemLogger.LogAsync("LogInRepository", "ValidateLogin", ex.ToString());
            }


            // Fallback: Validate from local database
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var user = await connection.QueryFirstOrDefaultAsync<LogInMaster>
            ("sp_ValidateLogin",
                 new { OperatorId = operatorId, OperatorPassword = password },
                 commandType: CommandType.StoredProcedure
            );


            if (user != null)
            {
                user.Shift = GetCurrentShift();
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

                if (setupStatus == "Setup Started" || machiningStatus == "Machining Started")
                    return (0, "Cannot logout. Setup or Machining is still in progress.");

                return (1, "Logout successful");
            }
            catch (Exception ex)
            {
                return (0, "Error during logout process: " + ex.Message);
            }
        }

        public string GetCurrentShift(DateTime? dateTime = null)
        {
            TimeSpan time = (dateTime ?? DateTime.Now).TimeOfDay;

            if (time >= new TimeSpan(7, 0, 0) && time < new TimeSpan(15, 30, 0))
                return "S1";
            else if (time >= new TimeSpan(15, 30, 0) && time <= new TimeSpan(23, 59, 59))
                return "S2";
            else
                return "S3";
        }

    }
}
