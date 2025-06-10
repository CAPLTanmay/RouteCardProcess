using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using RouteCardProcess.Model.Configurations;
using RouteCardProcess.Interfaces;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEmailAsync(string subject, string body)
    {
        await SendEmailAsync(subject, body, null);
    }

    public async Task SendEmailAsync(string subject, string body, List<string> toEmails = null)
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
}
