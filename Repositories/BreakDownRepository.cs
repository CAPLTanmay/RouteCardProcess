using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RouteCardProcess.Repositories
{
    public class BreakDownRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IEmailService _emailService;


        public BreakDownRepository(SqlConnectionFactory connectionFactory, IEmailService emailService)
        {
            _connectionFactory = connectionFactory;
            _emailService = emailService;
        }

        private SqlConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection();
        }

        public async Task<bool> StartBreakDownAsync(string workCenterNo, string operatorId, string? breakDownReasonCode = null)
        {
            using var connection = CreateConnection();
            var parameters = new
            {
                WorkCenterNo = workCenterNo,
                OperatorId = operatorId,
                BreakDownReasonCode = breakDownReasonCode
            };

            try
            {
                var rows = await connection.ExecuteAsync("sp_StartBreakDown", parameters, commandType: CommandType.StoredProcedure);

                if (rows > 0)
                {
                    var reasonText = string.IsNullOrEmpty(breakDownReasonCode) ? "Unknown Reason" : breakDownReasonCode;
                    var subject = $"[Breakdown Started] at {workCenterNo}";
                    var body = $"WorkCenter No: {workCenterNo} is in Breakdown due to <b>{reasonText}</b> at <b>{DateTime.Now:dd-MM-yyyy HH:mm:ss}</b>";

                    await _emailService.SendEmailAsync(subject, body, "tanmaysankpal119@gmail.com");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
                throw;
            }
        }

        public async Task<bool> EndBreakDownAsync(string workCenterNo, string? operatorId = null, string? breakDownReasonCode = null)
        {
            using var connection = CreateConnection();
            var parameters = new
            {
                WorkCenterNo = workCenterNo,
                OperatorId = operatorId,
                BreakDownReasonCode = breakDownReasonCode
            };

            var rows = await connection.ExecuteAsync("sp_EndBreakDown", parameters, commandType: CommandType.StoredProcedure);

            if (rows > 0)
            {
                var reasonText = string.IsNullOrEmpty(breakDownReasonCode) ? "Unknown Reason" : breakDownReasonCode;
                var subject = $"[Breakdown Ended] at {workCenterNo}";
                var body = $"WorkCenter No: {workCenterNo} Breakdown ended. Reason: <b>{reasonText}</b> at <b>{DateTime.Now:dd-MM-yyyy HH:mm:ss}</b>";

                await _emailService.SendEmailAsync(subject, body, "tanmaysankpal119@gmail.com");
                return true;
            }

            return false;
        }


    }
}
