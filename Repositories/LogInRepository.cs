using System.Data;
using Dapper;
using RouteCardProcess.Model;
using RouteCardProcess.Services;

namespace RouteCardProcess.Repositories
{
    public class LogInRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly SetUpTransRepository _setUpTransRepository;
        private readonly KblAuthService _kblService;
        private readonly IConfiguration _configuration;

        public LogInRepository(SqlConnectionFactory connectionFactory, SetUpTransRepository setUpTransRepository, KblAuthService kblService, IConfiguration configuration)
        {
            _connectionFactory = connectionFactory;
            _setUpTransRepository = setUpTransRepository;
            _kblService = kblService;
            _configuration = configuration;
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
            var useKblAuth = _configuration.GetValue<bool>("UseKblAuthAPI");

            if (useKblAuth)
            {
                try
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

                        if (kblAuthResponse == "Success")
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
                                    Role = emp.Designation,
                                    DepartmentId = 3,
                                    DepartmentName = emp.Deptnm,
                                    Shift = GetCurrentShift(),
                                    IsFromKBL = true
                                };
                            }
                        }
                    }
                }
                catch
                {
                    // Optional: log or handle KBL failure silently
                }
            }


            // Fall back to local DB validation
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            var user = await connection.QueryFirstOrDefaultAsync<LogInMaster>(
                "sp_ValidateLogin",
                new { OperatorId = operatorId, Password = password },
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
