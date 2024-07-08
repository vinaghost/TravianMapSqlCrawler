using MediatR;
using Microsoft.Extensions.Hosting;
using ServerCrawler.Commands;

namespace ServerCrawler
{
    public sealed class StartUp(IHostApplicationLifetime hostApplicationLifetime,
                                IMediator mediator)
        : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;
        private readonly IMediator _mediator = mediator;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var servers = await _mediator.Send(new DownloadCalendarCommand(), cancellationToken);
            await _mediator.Send(new UpdateCalendarsCommand(servers), cancellationToken);
            await _mediator.Send(new SendMessageDiscordCommand(), cancellationToken);

            _hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}