using System.Data;
using System.Text;
using Azure.Core;
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

            string notifNum = "";
            string notifStatus = "";

            try
            {
                // Step 0: Find EquipmentNo
                string? equipmentNo = await connection.ExecuteScalarAsync<string>("usp_GetEquipmentNoByWorkCenter", new { request.WorkCenterNo }, commandType: CommandType.StoredProcedure);

                if (string.IsNullOrWhiteSpace(equipmentNo))
                {
                    return new BreakDownResponse
                    {
                        IsDbSuccess = false,
                        IsMailSent = false,
                        IsSapPosted = false,
                        Message = "Unable to find Equipment Number for the given Work Center."
                    };
                }

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
                        notifStatus = sapResponse.NOTIF_STATUS;
                    }
                }
                catch (Exception ex)
                {
                    await _systemLogger.LogAsync("BreakDownRepository", "SAP_PostError", ex.ToString());
                }

                // If SAP failed, skip DB and mail
                if (!isSapPosted)
                {
                    return new BreakDownResponse
                    {
                        IsDbSuccess = false,
                        IsMailSent = false,
                        IsSapPosted = false,
                        Message = GetBreakdownStatusMessage(false, false, false)
                    };
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
                        BreakNotificationNo = notifNum,
                        BreakdownNotificationStatus = notifStatus
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
                    var mailTemplate = await connection.QueryFirstOrDefaultAsync<MailTemplateDto>(
                        "usp_GetOnlineBreakdownMailTemplate",
                        new { Group = "GP_BR2" },
                        commandType: CommandType.StoredProcedure);

                    if (mailTemplate != null)
                    {
                        string subject = (mailTemplate.MailSubject ?? "").Replace("{workCenterNo}", request.WorkCenterNo);
                        string body = (mailTemplate.MailBody ?? "")
                            .Replace("{workCenterNo}", request.WorkCenterNo)
                            .Replace("{reasonText}", request.BreakdownCode ?? "Unknown Reason")
                            .Replace("{Time}", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

                        await _emailService.SendEmailAsync(subject, body, mailTemplate.MailTo, mailTemplate.MailCC, mailTemplate.MailBCC, mailTemplate.MailFrom);
                        isMailSent = true;
                    }
                }
                catch (Exception mailEx)
                {
                    isMailSent = false;
                    await _systemLogger.LogAsync("BreakDownRepository", "Mail_SendError", mailEx.ToString());
                }

                return new BreakDownResponse
                {
                    IsDbSuccess = isDbSuccess,
                    IsMailSent = isMailSent,
                    IsSapPosted = isSapPosted,
                    Message = GetBreakdownStatusMessage(isSapPosted, isDbSuccess, isMailSent)
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
                    Message = "An unexpected error occurred while processing the breakdown request."
                };
            }
        }

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

                    // Step A: Get StartTime & EndTime using SP
                    var breakdownTimestamps = await connection.QueryFirstOrDefaultAsync<(DateTime? StartTime, DateTime? EndTime)>(
                        "usp_GetBreakdownTimestampsByNotifNo",
                        new { notifNum },
                        commandType: CommandType.StoredProcedure);

                    if (breakdownTimestamps.StartTime != null && breakdownTimestamps.EndTime != null)
                    {
                        // Step B: Calculate total duration in minutes
                        var totalMinutes = (breakdownTimestamps.EndTime.Value - breakdownTimestamps.StartTime.Value).TotalMinutes;

                        // Step C: Get threshold using SP
                        var thresholdMinutes = await connection.ExecuteScalarAsync<int>(
                            "usp_GetBreakdownThresholdMinutes",
                            new { TimeCategoryId = MinorBreakdownCategoryId },
                            commandType: CommandType.StoredProcedure);

                        // Step D: Decide Category
                        string category = totalMinutes <= thresholdMinutes ? "Minor Breakdown" : "Standard Breakdown";

                        // Step E: Get Status Description from SP
                        var statusDescription = await connection.ExecuteScalarAsync<string>(
                            "usp_GetBreakdownStatusDescription",
                            new { StatusCode = sapResponse.STATUS },
                            commandType: CommandType.StoredProcedure);

                        string statusToSave = !string.IsNullOrWhiteSpace(statusDescription)
                            ? statusDescription
                            : sapResponse.STATUS;

                        // Step F: Update TransBreakdown using SP
                        var rows = await connection.ExecuteAsync(
                            "usp_UpdateBreakdownOnEnd",
                            new { notifNum, Status = statusToSave, Category = category },
                            commandType: CommandType.StoredProcedure);

                        isDbSuccess = rows > 0;
                    }
                }

                // If SAP failed, skip DB update and mail
                if (!isSapPosted)
                {
                    return new BreakDownResponse
                    {
                        IsSapPosted = false,
                        IsDbSuccess = false,
                        IsMailSent = false,
                        Message = GetBreakdownStatusMessage(false, false, false)
                    };
                }

                // Step 3: Mail
                try
                {
                    // Step 3A: Get WorkCenterNo and BreakdownCode using SP
                    var breakdownInfo = await connection.QueryFirstOrDefaultAsync<(string WorkCenterNo, string BreakdownCode)>(
                        "usp_GetBreakdownDetailsByNotifNo",
                        new { notifNum },
                        commandType: CommandType.StoredProcedure);

                    var mailTemplate = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        "usp_GetOnlineBreakdownMailTemplate",
                        new { Group = "GP_BR3" },
                        commandType: CommandType.StoredProcedure);

                    if (mailTemplate != null)
                    {
                        string workCenterNo = breakdownInfo.WorkCenterNo ?? "Unknown";
                        string reasonText = breakdownInfo.BreakdownCode ?? "Unknown Reason";

                        string subject = (mailTemplate.MailSubject ?? "")
                            .Replace("{workCenterNo}", workCenterNo);

                        string body = (mailTemplate.MailBody ?? "")
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

                        isMailSent = true;
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
                    Message = GetBreakdownStatusMessage(isSapPosted, isDbSuccess, isMailSent)
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
                    Message = "An unexpected error occurred while ending the breakdown."
                };
            }
        }

        public async Task<IEnumerable<BreakDownRecordDto>> GetAllBreakDownsAsync()
        {
            using var connection = CreateConnection();

            // Step 1: Fetch breakdowns via SP
            var breakDownList = (await connection.QueryAsync<BreakDownRecordDto>(
                "usp_GetAllBreakdowns", commandType: CommandType.StoredProcedure)).ToList();

            // Step 2: Filter incomplete SAP statuses
            var pendingNotifNums = breakDownList
                .Where(b => !string.IsNullOrWhiteSpace(b.BreakNotificationNo)
                    && !string.Equals(b.BreakdownNotificationStatus, "Notification completed", StringComparison.OrdinalIgnoreCase))
                .Select(b => b.BreakNotificationNo)
                .Distinct()
                .ToList();

            if (!pendingNotifNums.Any())
                return breakDownList;

            // Step 3: Call SAP and sync data
            var sapResults = await _sapBreakdownService.GetBulkBreakdownStatusesAsync(pendingNotifNums);

            foreach (var sap in sapResults)
            {
                if (string.IsNullOrWhiteSpace(sap.NOTIF_NUM)) continue;

                var localRecords = breakDownList.Where(b => b.BreakNotificationNo == sap.NOTIF_NUM).ToList();
                if (!localRecords.Any()) continue;

                var sapStatus = sap.STATUS?.Trim();
                if (string.IsNullOrWhiteSpace(sapStatus)) continue;

                var statusDescription = await connection.ExecuteScalarAsync<string>(
                    "usp_GetBreakdownStatusDescription",
                    new { StatusCode = sapStatus },
                    commandType: CommandType.StoredProcedure);

                string finalStatusToSave = !string.IsNullOrWhiteSpace(statusDescription) ? statusDescription : sapStatus;

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
                            "usp_GetBreakdownThresholdMinutes",
                            new { TimeCategoryId = MinorCategoryId },
                            commandType: CommandType.StoredProcedure);

                        var durationMinutes = (sapEndDateTime.Value - record.BreakdownStartTime.Value).TotalMinutes;
                        newCategory = durationMinutes <= thresholdMinutes ? "Minor Breakdown" : "Major Breakdown";
                    }

                    if (statusChanged || canUpdateEndTime)
                    {
                        await connection.ExecuteAsync(
                            "usp_UpdateBreakdownStatusAndTime",
                            new
                            {
                                BreakNotificationNo = record.BreakNotificationNo,
                                Status = finalStatusToSave,
                                EndTime = sapEndDateTime,
                                Category = newCategory
                            },
                            commandType: CommandType.StoredProcedure
                        );

                        // Update DTO
                        record.BreakdownNotificationStatus = finalStatusToSave;
                        if (canUpdateEndTime) record.BreakdownEndTime = sapEndDateTime;
                        if (!string.IsNullOrWhiteSpace(newCategory)) record.BreakdownCategory = newCategory;
                    }
                }
            }

            // Step 4: Compute total time
            foreach (var record in breakDownList)
            {
                if (record.BreakdownStartTime.HasValue && record.BreakdownEndTime.HasValue)
                {
                    record.TotalBreakdownTime = record.BreakdownEndTime.Value - record.BreakdownStartTime.Value;
                }
                else
                {
                    record.TotalBreakdownTime = null;
                }
            }

            return breakDownList;
        }

        private string GetBreakdownStatusMessage(bool isSapPosted, bool isDbSuccess, bool isMailSent)
        {
            if (!isSapPosted && !isDbSuccess && !isMailSent)
                return "Breakdown failed. SAP, database, and email steps all unsuccessful.";

            if (isSapPosted && !isDbSuccess && !isMailSent)
                return "SAP posted. Database save and email failed.";

            if (isSapPosted && isDbSuccess && !isMailSent)
                return "SAP and database saved. Email failed.";

            if (isSapPosted && !isDbSuccess && isMailSent)
                return "SAP and email sent. Database save failed.";

            if (isSapPosted && isDbSuccess && isMailSent)
                return "Breakdown started. All steps successful.";

            return "Invalid state. Contact admin.";
        }


    }
}