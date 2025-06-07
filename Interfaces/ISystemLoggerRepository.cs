namespace RouteCardProcess.Interfaces
{
    public interface ISystemLoggerRepository
    {
        Task LogAsync(string moduleName, string functionName, string errorMessage);
    }
}
