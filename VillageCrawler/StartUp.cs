using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VillageCrawler.Commands;
using VillageCrawler.Entities;
using VillageCrawler.Extensions;

namespace VillageCrawler
{
    public sealed class StartUp(IHostApplicationLifetime hostApplicationLifetime,
                                IMediator mediator,
                                ILogger<StartUp> logger)
        : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<StartUp> _logger = logger;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var validServers = await _mediator.Send(new ValidateServerCommand(), cancellationToken);
            //var servers = new ConcurrentQueue<Server>();

            //await Parallel.ForEachAsync(validServers, async (serverUrl, token) =>
            //{
            //    var sw = new Stopwatch();
            //    sw.Start();
            //    var server = await UpdateVillageDatabase(serverUrl, cancellationToken);
            //    sw.Stop();
            //    if (server is null) return;
            //    servers.Enqueue(server);
            //    _logger.LogInformation("Updated {Url} in {Time}s", serverUrl, sw.ElapsedMilliseconds / 1000);
            //});

            //await _mediator.Send(new UpdateServerListCommand([.. servers]), cancellationToken);

            //var data = servers.OrderByDescending(x => x.PlayerCount).ToList();
            //ConsoleTable
            //    .From(data)
            //    .Configure(o => o.NumberAlignment = Alignment.Right)
            //    .Write(Format.Alternative);

            //_hostApplicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task<Server?> UpdateVillageDatabase(string url, CancellationToken cancellationToken)
        {
            var villages = await _mediator.Send(new DownloadMapSqlCommand(url), cancellationToken);
            if (villages.Count == 0) return null;

            using var context = await _mediator.Send(new CreateVillageDatabaseCommand(url), cancellationToken);

            var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await context.UpdateAlliance(villages, cancellationToken);
                await context.UpdatePlayer(villages, cancellationToken);
                await context.UpdateVillage(villages, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{Message}", e.Message);
                await transaction.RollbackAsync(cancellationToken);
            }

            var allianceCount = await context.Alliances.CountAsync(cancellationToken: cancellationToken);
            var playerCount = await context.Players.CountAsync(cancellationToken: cancellationToken);
            var villageCount = await context.Villages.CountAsync(cancellationToken: cancellationToken);

            var server = new Server
            {
                Url = url,
                LastUpdate = DateTime.Now,
                AllianceCount = allianceCount,
                PlayerCount = playerCount,
                VillageCount = villageCount,
            };

            return server;
        }
    }
}