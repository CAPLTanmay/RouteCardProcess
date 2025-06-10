namespace RouteCardProcess.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string subject, string body, List<string> toEmails);
        Task SendEmailAsync(string subject, string body);
    }
}
