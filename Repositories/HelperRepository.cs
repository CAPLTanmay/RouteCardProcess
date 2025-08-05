using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Helper;
using RouteCardProcess.Model.Entities;

namespace RouteCardProcess.Repositories
{
    public class HelperRepository:IHelperRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IUserMessageService _userMessageService;
        private readonly ILogInRepository _repo;
        public HelperRepository(SqlConnectionFactory connectionFactory, IUserMessageService userMessageService, ILogInRepository repo)
        {
            _connectionFactory = connectionFactory;
            _userMessageService = userMessageService;
            _repo = repo;
        }
        private SqlConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection();
        }
        public async Task<string> AddHelperAsync(HelperRequest request)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            // Fetch all users from the login table
            //var allUsers = await connection.QueryAsync<LogInMaster>(
            //    "usp_GetAllLogins",
            //    commandType: CommandType.StoredProcedure
            //);

            //var validUser = allUsers.FirstOrDefault(u =>
            //    u.OperatorId == request.OperatorId && u.OperatorPassword == request.Password
            //);
            var validUser = await _repo.LoginEmployeeAsync(request.OperatorId, request.Password);

            if (validUser.User == null)
                return _userMessageService.GetMessage(1001);

            // Prepare the parameters for adding a helper record
            var parameters = new DynamicParameters();
            parameters.Add("OperatorId", request.OperatorId);
            parameters.Add("SetupID", request.SetupId);
            parameters.Add("MachiningID", request.MachiningId);
            parameters.Add("OperatorStartTime", DateTime.Now);
            parameters.Add("MainOperatorId", request.MainOperatorId);
            parameters.Add("MSTIdleCode", request.MSTIdleCode);
            parameters.Add("WorkCenter", request.WorkCenter);

            // Set optional times depending on SetupID or MachiningID
            if (!string.IsNullOrEmpty(request.SetupId))
            {
                // Logic to calculate and store total setup time (example value)
                parameters.Add("OperatorTotalSetupTime", TimeSpan.Zero); // Adjust as needed
            }

            if (!string.IsNullOrEmpty(request.MachiningId))
            {
                // Logic to calculate and store total machining time (example value)
                parameters.Add("OperatorTotalMachiningTime", TimeSpan.Zero); // Adjust as needed
            }

            // Execute stored procedure to insert the helper record
            try
            {
                await connection.ExecuteAsync(
                    "usp_AddHelper",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return _userMessageService.GetMessage(1007); // Successfully added
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Helper already assigned and not released"))
                {
                    // Extract WorkCenter from error message
                    var match = Regex.Match(ex.Message, @"WorkCenter: (?<wc>\w+)");
                    var workCenter = match.Success ? match.Groups["wc"].Value : "Unknown";

                    // Optional: return full message
                    return $"Helper already engaged for WorkCenter: {workCenter}";

                    // OR if you want to fetch from DB message system
                    // return _userMessageService.GetMessage(1100).Replace("{WorkCenter}", workCenter);
                }

                return _userMessageService.GetMessage(5001); // Generic error
            }
        }
        public async Task<string> EndHelperAsync(EndHelperRequest request)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("OperatorId", request.OperatorId);
                parameters.Add("SetupId", string.IsNullOrEmpty(request.SetupId) ? null : request.SetupId);
                parameters.Add("MachiningId", string.IsNullOrEmpty(request.MachiningId) ? null : request.MachiningId);

                await connection.ExecuteAsync(
                    "usp_EndHelperSession",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return _userMessageService.GetMessage(1008);
            }
            catch (SqlException ex) when (ex.Message.Contains(_userMessageService.GetMessage(1009)))
            {
                return _userMessageService.GetMessage(1009);
            }
        }

        public async Task<string> ToggleHelperPauseAsync(EndHelperRequest request)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            // Find current log row
            var existing = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "usp_GetHelperBySetupAndMachiningId",
                new { request.OperatorId, request.SetupId, request.MachiningId },
                commandType: CommandType.StoredProcedure);

            if (existing == null)
                return _userMessageService.GetMessage(1009);

            if (existing.PauseStartTime == null)
            {
                // Start pause
                await connection.ExecuteAsync(
                    "usp_StartHelperPause",
                    new { request.OperatorId, request.SetupId, request.MachiningId, PauseStartTime = DateTime.Now },
                    commandType: CommandType.StoredProcedure);
                return _userMessageService.GetMessage(1010);
            }
            else
            {
                // Resume and calculate pause time
                var pauseStart = (DateTime)existing.PauseStartTime;
                var pauseEnd = DateTime.Now;
                var pauseDuration = pauseEnd - pauseStart;

                await connection.ExecuteAsync(
                    "usp_EndHelperPause",
                    new
                    {
                        request.OperatorId,
                        request.SetupId,
                        request.MachiningId,
                        PauseEndTime = pauseEnd,
                        PauseDuration = pauseDuration
                    },
                    commandType: CommandType.StoredProcedure);
                return _userMessageService.GetMessage(1011);
            }
        }

        public async Task<IEnumerable<OperatorHelperLog>> GetHelpersByMainOperatorIdAsync(string mainOperatorId)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("MainOperatorId", mainOperatorId);

            var result = await connection.QueryAsync<OperatorHelperLog>(
                "usp_GetHelpersByMainOperator",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return result.Where(x => !x.IsRelease);
        }
    }
}
