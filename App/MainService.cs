using App.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace App
{
    public class MainService : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly GetServerListCommand.Handler _getServerListCommand;
        private readonly ILogger<MainService> _logger;

        public MainService(IHostApplicationLifetime hostApplicationLifetime, GetServerListCommand.Handler getServerListCommand, ILogger<MainService> logger)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _getServerListCommand = getServerListCommand;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service is starting...");

            var servers = await _getServerListCommand.HandleAsync(new(), cancellationToken);
            if (servers.Count == 0)
            {
                _logger.LogWarning("No servers found.");
            }
            else
            {
                _logger.LogInformation("Found {count} servers:", servers.Count);
                _logger.LogInformation("{@servers}", servers);
            }

            _hostApplicationLifetime.StopApplication();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service is stopping...");
            await Task.CompletedTask;
        }
    }
}