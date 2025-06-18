using RouteCardProcess.Interfaces;

namespace RouteCardProcess.Repositories
{
    public static class UserMessageExtensions
    {
        public static string GetMessage(this IUserMessageService service, int code)
        {
            if (service.Messages.TryGetValue(code, out var msg))
                return msg;

            throw new KeyNotFoundException($"UserMessageCode {code} not found in UserMessageMaster table.");
        }
    }

}
