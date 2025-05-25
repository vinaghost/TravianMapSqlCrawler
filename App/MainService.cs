using App.Commands;
using App.Entities;
using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace App
{
    public class MainService : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MainService> _logger;

        public MainService(IHostApplicationLifetime hostApplicationLifetime, ILogger<MainService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var getServerListCommand = scope.ServiceProvider.GetRequiredService<GetServerListCommand.Handler>();
            var ServerUrls = await getServerListCommand.HandleAsync(new(), cancellationToken);

            var servers = new ConcurrentQueue<Server>();

            var mainSw = new Stopwatch();
            mainSw.Start();

            long totalRuntime = 0;
            var getMapSqlCommand = scope.ServiceProvider.GetRequiredService<GetMapSqlCommand.Handler>();
            var getVillageDataCommand = scope.ServiceProvider.GetRequiredService<GetVillageDataCommand.Handler>();
            var updateVillageDatabaseCommand = scope.ServiceProvider.GetRequiredService<UpdateVillageDatabaseCommand.Handler>();
            await Parallel.ForEachAsync(ServerUrls, async (ServerUrl, token) =>
            {
                var sw = new Stopwatch();
                sw.Start();
                var mapSql = await getMapSqlCommand.HandleAsync(new(ServerUrl), cancellationToken);
                var villages = await getVillageDataCommand.HandleAsync(new(mapSql), cancellationToken);
                var server = await updateVillageDatabaseCommand.HandleAsync(new(ServerUrl, villages), cancellationToken);
                sw.Stop();
                if (server is null) return;
                servers.Enqueue(server);
                totalRuntime += sw.ElapsedMilliseconds;
                _logger.LogInformation("Updated {Url} in {Time}s", ServerUrl, sw.ElapsedMilliseconds / 1000);
            });

            mainSw.Stop();

            _logger.LogInformation("Runtime: {Minutes}m {Seconds}s", mainSw.ElapsedMilliseconds / 1000 / 60, (mainSw.ElapsedMilliseconds / 1000) % 60);
            _logger.LogInformation("Total runtime of {count} servers: {Minutes}m {Seconds}s", servers.Count, totalRuntime / 1000 / 60, (totalRuntime / 1000) % 60);

            var data = servers.OrderByDescending(x => x.PlayerCount).ToList();
            ConsoleTable
               .From(data)
               .Configure(o => o.NumberAlignment = Alignment.Right)
               .Write(Format.Alternative);

            var updateServerListCommand = scope.ServiceProvider.GetRequiredService<UpdateServerListCommand.Handler>();
            await updateServerListCommand.HandleAsync(new([.. servers]), cancellationToken);

            _hostApplicationLifetime.StopApplication();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}