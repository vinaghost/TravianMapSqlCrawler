using MediatR;
using Microsoft.Extensions.Hosting;
using OasisCrawler.Commands;

namespace OasisCrawler
{
    public sealed class StartUp(
        IHostApplicationLifetime hostApplicationLifetime,
        IMediator mediator)
        : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;
        private readonly IMediator _mediator = mediator;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _mediator.Send(new CreateOasisDatabaseCommand("ts30.x3.europe.travian.com"), cancellationToken);
            await Task.CompletedTask;
            _hostApplicationLifetime.StopApplication();
        }

        private async Task CreateTable(CancellationToken cancellationToken)
        {
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}