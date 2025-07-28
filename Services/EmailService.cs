using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using RouteCardProcess.Model.Configurations;
using RouteCardProcess.Interfaces;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ISystemLoggerRepository _systemLogger;
    public EmailService(IOptions<EmailSettings> options, ISystemLoggerRepository systemLogger)
    {
        _settings = options.Value;
        _systemLogger = systemLogger;
    }

    public async Task SendEmailAsync(string subject, string body)
    {
        try
        {
            await SendEmailAsync(subject, body, null);
        }
        catch (Exception ex)
        {
            await _systemLogger.LogAsync("EmailService", "SendEmailAsync", ex.ToString());
            throw new ApplicationException("Error while sending email.", ex);
        }
    }

    public async Task SendEmailAsync(string subject, string body, List<string> toEmails = null)
    {
        try
        {
            var finalRecipients = (toEmails == null || toEmails.Count == 0)
                ? _settings.DefaultToEmails
                : toEmails;

            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            foreach (var email in finalRecipients.Distinct())
            {
                mail.To.Add(email);
            }

            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            await _systemLogger.LogAsync("EmailService", "SendEmailAsync", ex.ToString());
            throw new ApplicationException("Error while sending email with multiple recipients.", ex);
        }
    }

    public async Task SendEmailAsync(string subject, string body, string to, string cc = null, string bcc = null, string from = null)
    {
        try
        {
            var mail = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            if (!string.IsNullOrWhiteSpace(from)    )
                mail.From = new MailAddress(from);
            else
                mail.From = new MailAddress(_settings.SenderEmail, _settings.SenderName);

            if (!string.IsNullOrWhiteSpace(to))
            {
                var recipients = to.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (recipients.Length > 0)
                {
                    foreach (var address in recipients)
                    {
                        mail.To.Add(address.Trim());
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(cc))
            {
                var ccRecipients = cc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (ccRecipients.Length > 0)
                {
                    foreach (var address in ccRecipients)
                    {
                        mail.CC.Add(address.Trim());
                    }
                }
            }

            using var client = new SmtpClient
            {
                Host = _settings.SmtpServer,
                Port = _settings.SmtpPort,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = true,
                //Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            //client.Send(mail);

            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            await _systemLogger.LogAsync("EmailService", "SendEmailAsync", ex.ToString());
            throw new ApplicationException("Error while sending email with detailed parameters.", ex);
        }
    }
}
