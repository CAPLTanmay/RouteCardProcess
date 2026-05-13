using Microsoft.Extensions.Hosting;
using RouteCardProcess.Interfaces;

namespace RouteCardProcess.Services
{
    public sealed class ContractEmployeeAutoInactivationHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public ContractEmployeeAutoInactivationHostedService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var enabled = _configuration.GetValue("ContractEmployeeSettings:AutoInactivateExpiredEnabled", true);
            if (!enabled)
                return;

            var intervalMinutes = _configuration.GetValue("ContractEmployeeSettings:AutoInactivateExpiredIntervalMinutes", 60);
            if (intervalMinutes < 1)
                intervalMinutes = 60;

            await RunOnceAsync();

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnceAsync();
            }
        }

        private async Task RunOnceAsync()
        {
            using var scope = _scopeFactory.CreateScope();

            var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
            var systemLogger = scope.ServiceProvider.GetService<ISystemLoggerRepository>();

            try
            {
                var updated = await employeeRepository.InactivateExpiredContractEmployeesAsync(DateTime.Today);
                if (updated > 0 && systemLogger != null)
                {
                    await systemLogger.LogAsync(
                        "ContractEmployeeAutoInactivationHostedService",
                        "RunOnceAsync",
                        $"Inactivated {updated} expired contract employee(s) on {DateTime.Today:yyyy-MM-dd}.");
                }
            }
            catch (Exception ex)
            {
                if (systemLogger != null)
                {
                    await systemLogger.LogAsync(
                        "ContractEmployeeAutoInactivationHostedService",
                        "RunOnceAsync",
                        ex.ToString());
                }
            }
        }
    }
}
