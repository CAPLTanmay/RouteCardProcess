using System.Data;
using System.Text;
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
        public BreakDownRepository(SqlConnectionFactory connectionFactory, IEmailService emailService, ISystemLoggerRepository systemLogger, IValidationRepository sapBreakdownService)
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
                equipmentNo = await connection.ExecuteScalarAsync<string>("usp_GetEquipmentNoByWorkCenter", new { request.WorkCenterNo }, commandType: CommandType.StoredProcedure);

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
        // Repository Method for Breakdown End
        public async Task<BreakDownResponse> EndBreakDownAsync(string notifNum)
        {
            using var connection = CreateConnection();

            bool isSapPosted = false;
            bool isDbSuccess = false;
            bool isMailSent = false;

            try
            {
                // Step 1: SAP Close Notification
                var sapClosePayload = new SAPBreakdownCloseRequest
                {
                    NOTIF_NUM = notifNum,
                    STATUS = ""
                };

                var sapResponse = await _sapBreakdownService.PostBreakdownCloseAsync(sapClosePayload);
                if (sapResponse != null)
                {
                    isSapPosted = true;
                    const int MinorBreakdownCategoryId = 1;


                    // Step A: Get Start Time
                    var breakdownTimestamps = await connection.QueryFirstOrDefaultAsync<(DateTime? StartTime, DateTime? EndTime)>(
                        "SELECT BreakdownStartTime AS StartTime, GETDATE() AS EndTime FROM TransBreakdown WHERE BreakNotificationNo = @notifNum",
                        new { notifNum });

                    if (breakdownTimestamps.StartTime != null && breakdownTimestamps.EndTime != null)
                    {
                        // Step B: Calculate total duration in minutes
                        var totalMinutes = (breakdownTimestamps.EndTime.Value - breakdownTimestamps.StartTime.Value).TotalMinutes;

                        // Step C: Get 30-minute threshold from MSTBreakdownTimeCategory (smallest one)
                        var thresholdMinutes = await connection.ExecuteScalarAsync<int>(@"SELECT DATEDIFF(MINUTE, '00:00:00', BrekdownTime) FROM MSTBreakdownTimeCategory WHERE TimeCategoryId = @Id",
     new { Id = MinorBreakdownCategoryId });


                        // Step D: Decide category in C#
                        string category = totalMinutes <= thresholdMinutes ? "Minor Breakdown" : "Major Breakdown";

                        // Step E: Get Description from MSTBreakdownStatus
                        var statusDescription = await connection.ExecuteScalarAsync<string>(
                            @"SELECT BreakdownStatusDescription FROM MSTBreakdownStatus WHERE BreakdownStatus = @StatusCode",
                            new { StatusCode = sapResponse.STATUS });

                        //  Save as description if found, else fallback to raw STATUS code
                        string statusToSave = !string.IsNullOrWhiteSpace(statusDescription)
                            ? statusDescription
                            : sapResponse.STATUS;

                        // Step F: Update TransBreakdown
                        var rows = await connection.ExecuteAsync(
                            @"UPDATE TransBreakdown SET BreakdownEndTime = GETDATE(), BreakdownNotificationStatus = @Status, BreakdownCategory = @Category WHERE BreakNotificationNo = @notifNum",
                            new { notifNum, Status = statusToSave, Category = category });


                        isDbSuccess = rows > 0;
                    }

                }

                // Step 3: Mail
                try
                {
                    var mailTemplate = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "usp_GetOnlineBreakdownMailTemplate",
                        new { Group = "GP_BR_END" },
                        commandType: CommandType.StoredProcedure);

                    if (mailTemplate != null)
                    {
                        string subject = (mailTemplate.MailSubject ?? "").Replace("{notifNum}", notifNum);
                        string body = (mailTemplate.MailBody ?? "").Replace("{notifNum}", notifNum).Replace("{Time}", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

                        isMailSent = await _emailService.SendEmailAsync(subject, body, mailTemplate.MailTo, mailTemplate.MailCC, mailTemplate.MailBCC, mailTemplate.MailFrom);
                    }
                }
                catch (Exception mailEx)
                {
                    await _systemLogger.LogAsync("BreakDownRepository", "Mail_SendError_EndBreak", mailEx.ToString());
                }

                return new BreakDownResponse
                {
                    IsSapPosted = isSapPosted,
                    IsDbSuccess = isDbSuccess,
                    IsMailSent = isMailSent,
                    Message = $"SAP: {(isSapPosted ? "Success" : "Fail")}, DB: {(isDbSuccess ? "Success" : "Fail")}, Mail: {(isMailSent ? "Success" : "Fail")}"
                };
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakDownRepository", "EndBreakDownAsync", ex.ToString());

                return new BreakDownResponse
                {
                    IsSapPosted = false,
                    IsDbSuccess = false,
                    IsMailSent = false,
                    Message = "Exception occurred while ending breakdown."
                };
            }
        }
        public async Task<IEnumerable<BreakDownRecordDto>> GetAllBreakDownsAsync()
        {
            using var connection = CreateConnection();

            // Step 1: Fetch initial breakdowns
            var breakDownList = (await connection.QueryAsync<BreakDownRecordDto>(
                @"SELECT TOP (1000) 
            WorkCenterNo,
            OperatorId,
            BreakdownCategory,
            BreakdownCodeGroup,
            BreakdownCode,
            BreakNotificationNo,
            BreakdownNotificationStatus,
            BreakdownStartTime,
            BreakdownEndTime,
            TotalBreakdownTime,
            EquipmentNo
        FROM TransBreakdown
        ORDER BY BreakdownStartTime DESC")).ToList();

            // Step 2: Filter out records not yet completed in SAP
            var pendingNotifNums = breakDownList
                .Where(b =>
                    !string.IsNullOrWhiteSpace(b.BreakNotificationNo) &&
                    !string.Equals(b.BreakdownNotificationStatus, "Notification completed", StringComparison.OrdinalIgnoreCase))
                .Select(b => b.BreakNotificationNo)
                .Distinct()
                .ToList();

            if (!pendingNotifNums.Any())
                return breakDownList;

            // Step 3: Fetch SAP statuses in bulk
            var sapResults = await _sapBreakdownService.GetBulkBreakdownStatusesAsync(pendingNotifNums);

            foreach (var sap in sapResults)
            {
                var notifNum = sap.NOTIF_NUM;
                if (string.IsNullOrWhiteSpace(notifNum)) continue;

                var localRecords = breakDownList.Where(b => b.BreakNotificationNo == notifNum).ToList();
                if (!localRecords.Any()) continue;

                var sapStatus = sap.STATUS?.Trim();
                if (string.IsNullOrWhiteSpace(sapStatus)) continue;

                var statusDescription = await connection.ExecuteScalarAsync<string>(
                    @"SELECT BreakdownStatusDescription 
              FROM MSTBreakdownStatus 
              WHERE BreakdownStatus = @StatusCode",
                    new { StatusCode = sapStatus });

                string finalStatusToSave = !string.IsNullOrWhiteSpace(statusDescription) ? statusDescription : sapStatus;

                // Parse SAP end datetime if both fields exist
                DateTime? sapEndDateTime = null;
                if (!string.IsNullOrWhiteSpace(sap.NOTIF_CLOSE_DATE) && !string.IsNullOrWhiteSpace(sap.NOTIF_CLOSE_TIME))
                {
                    DateTime.TryParse($"{sap.NOTIF_CLOSE_DATE} {sap.NOTIF_CLOSE_TIME}", out var parsedEnd);
                    if (parsedEnd != DateTime.MinValue)
                        sapEndDateTime = parsedEnd;
                }

                foreach (var record in localRecords)
                {
                    if (string.IsNullOrWhiteSpace(record.BreakNotificationNo)) continue;

                    bool statusChanged = !string.Equals(record.BreakdownNotificationStatus, finalStatusToSave, StringComparison.OrdinalIgnoreCase);
                    bool canUpdateEndTime = sapEndDateTime.HasValue && !record.BreakdownEndTime.HasValue;

                    string? newCategory = null;

                    if (canUpdateEndTime && record.BreakdownStartTime.HasValue)
                    {
                        const int MinorCategoryId = 1;

                        var thresholdMinutes = await connection.ExecuteScalarAsync<int>(
                            @"SELECT DATEDIFF(MINUTE, '00:00:00', BrekdownTime) 
                      FROM MSTBreakdownTimeCategory 
                      WHERE TimeCategoryId = @Id",
                            new { Id = MinorCategoryId });

                        var durationMinutes = (sapEndDateTime.Value - record.BreakdownStartTime.Value).TotalMinutes;
                        newCategory = durationMinutes <= thresholdMinutes ? "Minor Breakdown" : "Major Breakdown";
                    }

                    if (statusChanged || canUpdateEndTime)
                    {
                        var updateQuery = new StringBuilder("UPDATE TransBreakdown SET BreakdownNotificationStatus = @Status");
                        if (canUpdateEndTime) updateQuery.Append(", BreakdownEndTime = @EndTime");
                        if (!string.IsNullOrWhiteSpace(newCategory)) updateQuery.Append(", BreakdownCategory = @Category");

                        updateQuery.Append(" WHERE BreakNotificationNo = @notifNum");

                        await connection.ExecuteAsync(updateQuery.ToString(), new
                        {
                            Status = finalStatusToSave,
                            EndTime = sapEndDateTime,
                            Category = newCategory,
                            notifNum = record.BreakNotificationNo
                        });

                        // Update DTO
                        record.BreakdownNotificationStatus = finalStatusToSave;
                        if (canUpdateEndTime) record.BreakdownEndTime = sapEndDateTime;
                        if (!string.IsNullOrWhiteSpace(newCategory)) record.BreakdownCategory = newCategory;
                    }
                }
            }

            // Step 4: Compute TotalBreakdownTime in DTO
            foreach (var record in breakDownList)
            {
                if (record.BreakdownStartTime.HasValue && record.BreakdownEndTime.HasValue)
                {
                    record.TotalBreakdownTime =
                        record.BreakdownEndTime.Value - record.BreakdownStartTime.Value;   // TimeSpan
                }
                else
                {
                    record.TotalBreakdownTime = null;
                }
            }


            return breakDownList;
        }


    }
}