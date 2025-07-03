using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.BreakDownDto;
using RouteCardProcess.Model.DTOs.SapValidation;

namespace RouteCardProcess.Repositories
{
    public class BreakDownRepository : IBreakDownRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IEmailService _emailService;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IValidationRepository _sapBreakdownService;
        public BreakDownRepository(SqlConnectionFactory connectionFactory, IEmailService emailService,ISystemLoggerRepository systemLogger, IValidationRepository sapBreakdownService)
        {
            _connectionFactory = connectionFactory;
            _emailService = emailService;
            _systemLogger = systemLogger;
            _sapBreakdownService = sapBreakdownService;
        }

        private SqlConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection();
        }

        public async Task<BreakDownResponse> StartBreakDownAsync(BreakDownStartRequest request)
        {
            using var connection = CreateConnection();

            bool isDbSuccess = false, isMailSent = false, isSapPosted = false;
            string equipmentNo = "";
            string notifNum = "";

            try
            {
                // Step 0: Find EquipmentNo
                equipmentNo = await connection.ExecuteScalarAsync<string>(
     "usp_GetEquipmentNoByWorkCenter",
     new { request.WorkCenterNo },
     commandType: CommandType.StoredProcedure
 );

                // Step 1: SAP POST
                try
                {
                    var sapPayload = new SAPBreakdownRequest
                    {
                        WORKCENTER = request.WorkCenterNo,
                        EQUIPMENT = equipmentNo,
                        CODE_GRP = request.BreakdownCodeGroup ?? "",
                        CODE = request.BreakdownCode ?? "",
                        BRKDWN_DATE = DateTime.Now.ToString("yyyy-MM-dd"),
                        BRKDWN_TIME = DateTime.Now.ToString("HH:mm:ss")
                    };

                    var sapResponse = await _sapBreakdownService.PostBreakdownAsync(sapPayload);
                    if (sapResponse != null)
                    {
                        isSapPosted = true;
                        notifNum = sapResponse.NOTIF_NUM;
                    }
                }
                catch (Exception ex)
                {
                    await _systemLogger.LogAsync("BreakDownRepository", "SAP_PostError", ex.ToString());
                }

                // Step 2: DB Insert
                try
                {
                    var dbParams = new
                    {
                        request.WorkCenterNo,
                        request.OperatorId,
                        request.BreakdownCodeGroup,
                        request.BreakdownCode,
                        EquipmentNo = equipmentNo,
                        BreakNotificationNo = notifNum
                    };

                    var rows = await connection.ExecuteAsync("usp_StartBreakDown", dbParams, commandType: CommandType.StoredProcedure);
                    isDbSuccess = rows > 0;
                }
                catch (Exception dbEx)
                {
                    await _systemLogger.LogAsync("BreakDownRepository", "DB_InsertError", dbEx.ToString());
                }

                // Step 3: Email
                try
                {
                    var mailTemplate = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "usp_GetOnlineBreakdownMailTemplate",
                        new { Group = "GP_BR" },
                        commandType: CommandType.StoredProcedure);

                    if (mailTemplate != null)
                    {
                        string subject = (mailTemplate.MailSubject ?? "").Replace("{workCenterNo}", request.WorkCenterNo);
                        string body = (mailTemplate.MailBody ?? "")
                            .Replace("{workCenterNo}", request.WorkCenterNo)
                            .Replace("{reasonText}", request.BreakdownCode ?? "Unknown Reason")
                            .Replace("{Time}", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

                        isMailSent = await _emailService.SendEmailAsync(subject, body, mailTemplate.MailTo, mailTemplate.MailCC, mailTemplate.MailBCC, mailTemplate.MailFrom);
                    }
                }
                catch (Exception mailEx)
                {
                    await _systemLogger.LogAsync("BreakDownRepository", "Mail_SendError", mailEx.ToString());
                }

                return new BreakDownResponse
                {
                    IsDbSuccess = isDbSuccess,
                    IsMailSent = isMailSent,
                    IsSapPosted = isSapPosted,
                    Message = $"SAP: {(isSapPosted ? "Success" : "Fail")}, DB: {(isDbSuccess ? "Success" : "Fail")}, Mail: {(isMailSent ? "Success" : "Fail")}"
                };
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakDownRepository", "StartBreakDownAsync", ex.ToString());

                return new BreakDownResponse
                {
                    IsDbSuccess = false,
                    IsMailSent = false,
                    IsSapPosted = false,
                    Message = "Exception occurred while processing breakdown."
                };
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
