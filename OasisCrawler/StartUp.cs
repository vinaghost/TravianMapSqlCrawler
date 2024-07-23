using Microsoft.Extensions.Hosting;

namespace OasisCrawler
{
    public sealed class StartUp(IHostApplicationLifetime hostApplicationLifetime)
        : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}