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

                    var subject = $"Breakdown Alert: Work Center {workCenterNo} is Down";

                    var body = $@"
<p><strong>Attention:</strong></p>
<p>The work center <strong>{workCenterNo}</strong> has encountered a <strong>breakdown</strong>.</p>
<p><strong>Reason Code:</strong> {reasonText}</p>
<p><strong>Time:</strong> {DateTime.Now:dd-MM-yyyy HH:mm:ss}</p>
<p>Please take immediate action to investigate and resolve the issue.</p>
";

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

                var subject = $"Breakdown Resolved: Work Center {workCenterNo} is Back Online";

                var body = $@"
<p><strong>Update:</strong></p>
<p>The breakdown at <strong>Work Center {workCenterNo}</strong> has been resolved.</p>
<p><strong>Reason Code:</strong> {reasonText}</p>
<p><strong>Resolved At:</strong> {DateTime.Now:dd-MM-yyyy HH:mm:ss}</p>
<p>The work center is now operational.</p>
";

                await _emailService.SendEmailAsync(subject, body, "tanmaysankpal119@gmail.com");
                return true;
            }

            return false;
        }
    }
}
