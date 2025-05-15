using System.Data;
using Dapper;
using RouteCardProcess.Model;

namespace RouteCardProcess.Repositories
{
    public class LogInRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly SetUpTransRepository _setUpTransRepository;

        public LogInRepository(SqlConnectionFactory connectionFactory, SetUpTransRepository setUpTransRepository)
        {
            _connectionFactory = connectionFactory;
            _setUpTransRepository = setUpTransRepository;
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
            try
            {
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
                }
                return user;
            }
            catch (Exception ex)
            {
                throw new Exception("Error validating login credentials.", ex);
            }
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
