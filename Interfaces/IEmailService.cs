namespace RouteCardProcess.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string subject, string body, List<string> toEmails);
        Task SendEmailAsync(string subject, string body);
        Task SendEmailAsync(string subject, string body, string to, string cc = null, string bcc = null, string from = null);

    }
}
