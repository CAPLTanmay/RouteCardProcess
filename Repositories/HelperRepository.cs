using System.Data;
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

        public HelperRepository(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
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
            var allUsers = await connection.QueryAsync<LogInMaster>(
                "sp_GetAllLogins",
                commandType: CommandType.StoredProcedure
            );

            var validUser = allUsers.FirstOrDefault(u =>
                u.OperatorId == request.OperatorId && u.OperatorPassword == request.Password
            );

            if (validUser == null)
                return "Invalid Operator ID or Password.";

            // Prepare the parameters for adding a helper record
            var parameters = new DynamicParameters();
            parameters.Add("OperatorId", request.OperatorId);
            parameters.Add("SetupID", request.SetupId);
            parameters.Add("MachiningID", request.MachiningId);
            parameters.Add("OperatorStartTime", DateTime.Now);
            parameters.Add("MainOperatorId", request.MainOperatorId);

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
            await connection.ExecuteAsync(
                "sp_AddHelper",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return "Helper added successfully.";
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
                    "sp_EndHelperSession",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return "Helper end time updated and released successfully.";
            }
            catch (SqlException ex) when (ex.Message.Contains("Helper record not found"))
            {
                return "Helper record not found.";
            }
        }

        public async Task<string> ToggleHelperPauseAsync(EndHelperRequest request)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            // Find current log row
            var existing = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_GetHelperBySetupAndMachiningId",
                new { request.OperatorId, request.SetupId, request.MachiningId },
                commandType: CommandType.StoredProcedure);

            if (existing == null)
                return "Helper record not found.";

            if (existing.PauseStartTime == null)
            {
                // Start pause
                await connection.ExecuteAsync(
                    "sp_StartHelperPause",
                    new { request.OperatorId, request.SetupId, request.MachiningId, PauseStartTime = DateTime.Now },
                    commandType: CommandType.StoredProcedure);
                return "Helper paused.";
            }
            else
            {
                // Resume and calculate pause time
                var pauseStart = (DateTime)existing.PauseStartTime;
                var pauseEnd = DateTime.Now;
                var pauseDuration = pauseEnd - pauseStart;

                await connection.ExecuteAsync(
                    "sp_EndHelperPause",
                    new
                    {
                        request.OperatorId,
                        request.SetupId,
                        request.MachiningId,
                        PauseEndTime = pauseEnd,
                        PauseDuration = pauseDuration
                    },
                    commandType: CommandType.StoredProcedure);
                return "Helper resumed.";
            }
        }

        public async Task<IEnumerable<OperatorHelperLog>> GetHelpersByMainOperatorIdAsync(string mainOperatorId)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("MainOperatorId", mainOperatorId);

            var result = await connection.QueryAsync<OperatorHelperLog>(
                "sp_GetHelpersByMainOperator",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return result.Where(x => !x.IsRelease);
        }

    }
}
