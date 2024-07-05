using ConsoleApplication.DbContexts;
using ConsoleApplication.Models.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsoleApplication
{
    public sealed class StartUp(IHostApplicationLifetime hostApplicationLifetime,
                                ILogger<StartUp> logger,
                                IOptions<ConnectionStrings> connections,
                                IOptions<AppSettings> appsettings)
        : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;
        private readonly ILogger<StartUp> _logger = logger;
        private readonly ConnectionStrings _connections = connections.Value;
        private readonly AppSettings _appSettings = appsettings.Value;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var serverContext = new ServerDbContext(_connections.Server);
            await serverContext.Database.EnsureCreatedAsync(cancellationToken);
            _logger.LogInformation("Server database created");

            using var villageContext = new VillageDbContext(_connections.Village, _appSettings.Servers[0]);
            await villageContext.Database.EnsureCreatedAsync(cancellationToken);
            _logger.LogInformation("Village database created");

            _hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}