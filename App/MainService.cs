using Microsoft.Extensions.Hosting;

namespace App
{
    public class MainService : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public MainService(IHostApplicationLifetime hostApplicationLifetime)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Service is starting...");
            _hostApplicationLifetime.StopApplication();
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Service is stopping...");
            await Task.CompletedTask;
        }
    }
}