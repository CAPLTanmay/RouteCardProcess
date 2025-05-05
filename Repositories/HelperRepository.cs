using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Model;

namespace RouteCardProcess.Repositories
{
    public class HelperRepository
    {
        private readonly IConfiguration _config;

        public HelperRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
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

            // Check if operator credentials are valid
            var validUser = allUsers.FirstOrDefault(u =>
                u.OperatorId == request.OperatorId && u.Password == request.Password
            );

            if (validUser == null)
                return "Invalid Operator ID or Password.";

            // Prepare the parameters for adding a helper record
            var parameters = new DynamicParameters();
            parameters.Add("OperatorId", request.OperatorId);
            parameters.Add("SetupID", request.SetupId);
            parameters.Add("MachiningID", request.MachiningId);
            parameters.Add("OperatorStartTime", DateTime.Now);

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

            // Check if the helper record exists
            var helper = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_GetHelperBySetupAndMachiningId",
                new { request.OperatorId, request.SetupId, request.MachiningId },
                commandType: CommandType.StoredProcedure
            );

            if (helper == null)
                return "Helper record not found.";

            // Set current time as the end time
            var operatorStartTime = helper.OperatorStartTime;
            var operatorEndTime = DateTime.Now;  // Current time when the end helper request is made
            var totalTimeSpent = operatorEndTime - operatorStartTime;

            // Determine if SetupID or MachiningID exists and update the corresponding column
            string updateQuery;
            if (!string.IsNullOrEmpty(request.SetupId))
            {
                updateQuery = "UPDATE Operator_Helper_Log SET OperatorEndTime = @OperatorEndTime, OperatorTotalSetupTime = @TotalTimeSpent WHERE OperatorId = @OperatorId AND SetupId = @SetupID";
            }
            else if (!string.IsNullOrEmpty(request.MachiningId))
            {
                updateQuery = "UPDATE Operator_Helper_Log SET OperatorEndTime = @OperatorEndTime, OperatorTotalMachiningTime = @TotalTimeSpent WHERE OperatorId = @OperatorId AND MachiningId = @MachiningID";
            }
            else
            {
                return "Neither SetupID nor MachiningID provided.";
            }

            // Prepare the parameters to update the helper record
            var parameters = new DynamicParameters();
            parameters.Add("OperatorId", request.OperatorId);
            parameters.Add("OperatorEndTime", operatorEndTime);
            parameters.Add("TotalTimeSpent", totalTimeSpent);
            parameters.Add("SetupID", request.SetupId);
            parameters.Add("MachiningID", request.MachiningId);

            // Execute the update query
            await connection.ExecuteAsync(updateQuery, parameters);

            return "Helper end time updated successfully.";
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
    }
}
