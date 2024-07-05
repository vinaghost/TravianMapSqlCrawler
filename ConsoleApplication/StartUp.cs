using ConsoleApplication.Models.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsoleApplication
{
    public sealed class StartUp(IHostApplicationLifetime hostApplicationLifetime,
                                ILogger<StartUp> logger,
                                IOptions<AppSettings> appSettings,
                                IOptions<ConnectionStrings> connections)
        : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;
        private readonly ILogger<StartUp> _logger = logger;
        private readonly AppSettings _appSettings = appSettings.Value;
        private readonly ConnectionStrings _connections = connections.Value;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("connections: {@Connections}", _connections);
            _logger.LogInformation("appSettings single: {@AppSettings}", _appSettings);

            _hostApplicationLifetime.StopApplication();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}