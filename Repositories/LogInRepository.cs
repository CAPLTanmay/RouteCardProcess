using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Model;
using System.Data;


namespace RouteCardProcess.Repositories
{
    public class LogInRepository
    {
        private readonly IConfiguration _config;
        private readonly SetUpTransRepository _setUpTransRepository;


        public LogInRepository(IConfiguration config, SetUpTransRepository setUpTransRepository)
        {
            _config = config;
            _setUpTransRepository = setUpTransRepository;
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        }

        public async Task<IEnumerable<LogInMaster>> GetAllAsync()
        {
            try
            {
                using var connection = CreateConnection();
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
                using var connection = CreateConnection();
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
                using var connection = CreateConnection();
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
                // Check setup and machining status
                var (flag, setupStatus, machiningStatus, message, setupIdFromDb, machiningId) = await _setUpTransRepository.CheckSetupNotificationStatusAsync(workCenterNo, workOrderNo, operationNo);

                // If setup or machining is not found, return an error flag and message
                if (flag == 0)
                    return (0, message);

                // Check if setup or machining are still in progress (or other relevant statuses)
                if (setupStatus == "Setup Started"|| machiningStatus == "Machining Started" )
                    return (0, "Cannot logout. Setup or Machining is still in progress.");

                // If all checks pass, return a flag indicating successful logout and a success message
                return (1, "Logout successful");
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors and return an error flag and message
                return (0, "Error during logout process: " + ex.Message);
            }
        }
        private string GetCurrentShift()
        {
            TimeSpan now = DateTime.Now.TimeOfDay;

            var s1Start = new TimeSpan(7, 0, 0);
            var s1End = new TimeSpan(15, 30, 0);

            var s2Start = new TimeSpan(15, 30, 0);
            var s2End = new TimeSpan(23, 59, 59);

            var s3Start = new TimeSpan(0, 0, 0);
            var s3End = new TimeSpan(7, 0, 0);

            if (now >= s1Start && now < s1End)
                return "S1";
            else if (now >= s2Start && now <= s2End)
                return "S2";
            else
                return "S3";
        }


    }
}
