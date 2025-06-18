using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;

namespace RouteCardProcess.Repositories
{
    public class BreakDownRepository : IBreakDownRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IEmailService _emailService;
        private readonly ISystemLoggerRepository _systemLogger;

        public BreakDownRepository(SqlConnectionFactory connectionFactory, IEmailService emailService,ISystemLoggerRepository systemLogger)
        {
            _connectionFactory = connectionFactory;
            _emailService = emailService;
            _systemLogger = systemLogger;
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
                var rows = await connection.ExecuteAsync("usp_StartBreakDown", parameters, commandType: CommandType.StoredProcedure);

                if (rows > 0)
                {
                    // Fetch mail template from MailMaster
                    var mailTemplate = await connection.QueryFirstOrDefaultAsync<dynamic>(
                     "usp_GetOnlineBreakdownMailTemplate",new { Group = "GP_BR" },commandType: CommandType.StoredProcedure);

                    if (mailTemplate != null)
                    {
                        string reasonText = string.IsNullOrEmpty(breakDownReasonCode) ? "Unknown Reason" : breakDownReasonCode;

                        // Replace tokens if present
                        string subject = mailTemplate.MailSubject;
                        subject = subject.Replace("{workCenterNo}", workCenterNo);
                        string body = mailTemplate.MailBody
                            .Replace("{workCenterNo}", workCenterNo)
                            .Replace("{reasonText}", reasonText)
                            .Replace("{Time}", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

                        await _emailService.SendEmailAsync(
                            subject,
                            body,
                            mailTemplate.MailTo,
                            mailTemplate.MailCC,
                            mailTemplate.MailBCC,
                            mailTemplate.MailFrom
                        );

                        return true;
                    }
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakDownRepository", "StartBreakDownAsync", ex.ToString());
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

            var rows = await connection.ExecuteAsync("usp_EndBreakDown", parameters, commandType: CommandType.StoredProcedure);

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

                await _emailService.SendEmailAsync(subject, body);
                return true;
            }

            return false;
        }
    }
}
