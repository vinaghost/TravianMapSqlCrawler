using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerCrawler.Commands;
using ServerCrawler.Models.Options;

namespace ServerCrawler
{
    public sealed class StartUp(IHostApplicationLifetime hostApplicationLifetime,
                                IMediator mediator,
                                ILogger<StartUp> logger,
                                IOptions<AppSettings> appsettings)
        : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<StartUp> _logger = logger;
        private readonly AppSettings _appSettings = appsettings.Value;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _mediator.Send(new DownloadCalendarCommand(), cancellationToken);
            //_hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}