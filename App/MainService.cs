using App.Commands;
using App.Entities;
using App.Models;
using ConsoleTables;
using CSharpDiscordWebhook;
using CSharpDiscordWebhook.Objects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetTopologySuite.GeometriesGraph;
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Drawing;

namespace App
{
    public class MainService : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MainService> _logger;
        private readonly IConfiguration _configuration;

        public MainService(IHostApplicationLifetime hostApplicationLifetime, ILogger<MainService> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var getServerListCommand = scope.ServiceProvider.GetRequiredService<GetServerListCommand.Handler>();
            var serverUrls = await getServerListCommand.HandleAsync(new(), cancellationToken);

            var getMapSqlCommand = scope.ServiceProvider.GetRequiredService<GetMapSqlCommand.Handler>();
            var getRawVillageCommand = scope.ServiceProvider.GetRequiredService<GetRawVillageCommand.Handler>();
            var rawVillageRecords = await GetRawVillages(serverUrls, getMapSqlCommand, getRawVillageCommand, cancellationToken);

            ConsoleTable
               .From(rawVillageRecords.Select(r => new { r.ServerUrl, r.FetchingRuntime, r.ParsingRuntime })
                   .Append(new
                   {
                       ServerUrl = $"Total [{rawVillageRecords.Length}]",
                       FetchingRuntime = rawVillageRecords.Select(x => x.FetchingRuntime).Aggregate(TimeSpan.Zero, (total, next) => total.Add(next)),
                       ParsingRuntime = rawVillageRecords.Select(x => x.ParsingRuntime).Aggregate(TimeSpan.Zero, (total, next) => total.Add(next)),
                   }))
               .Configure(o => o.NumberAlignment = Alignment.Right)
               .Write(Format.Minimal);

            var updateDatabaseCommand = scope.ServiceProvider.GetRequiredService<UpdateDatabaseCommand.Handler>();
            var serverRecords = await GetServerRecords(rawVillageRecords, updateDatabaseCommand, cancellationToken);

            ConsoleTable
               .From(serverRecords.OrderByDescending(x => x.Server.PlayerCount).Select(r => new { r.Server.Url, r.Server.VillageCount, r.Server.PlayerCount, r.Server.AllianceCount, r.Runtime })
                   .Append(new
                   {
                       Url = $"Total [{serverRecords.Length}]",
                       VillageCount = 0,
                       PlayerCount = 0,
                       AllianceCount = 0,
                       Runtime = serverRecords.Select(x => x.Runtime).Aggregate(TimeSpan.Zero, (total, next) => total.Add(next)),
                   }))
               .Configure(o => o.NumberAlignment = Alignment.Right)
               .Write(Format.Minimal);

            var servers = serverRecords
                .OrderByDescending(x => x.Server.PlayerCount)
                .Select(x => x.Server.Url.Replace("travian.com", ""))
                .ToList();
            var players = serverRecords
                .OrderByDescending(x => x.Server.PlayerCount)
                .Select(x => $"{x.Server.PlayerCount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)}")
                .ToList();
            var villages = serverRecords
                .OrderByDescending(x => x.Server.PlayerCount)
                .Select(x => $"{x.Server.VillageCount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)}")
                .ToList();

            using var webhook = new DiscordWebhook(new Uri(_configuration["DiscordWebhookUrl"]!));
            await webhook.SendMessageAsync(new MessageBuilder
            {
                Embeds =
                [
                    new EmbedBuilder
                    {
                        Title = "Run successfully",
                        Description = $"Update at <t:{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}:f>",
                        Fields = [
                            new EmbedFieldBuilder()
                            {
                                Name = "Server",
                                Value = string.Join("\n", servers),
                                Inline = true,
                            },
                            new EmbedFieldBuilder()
                            {
                                Name = "Player count",
                                Value = string.Join("\n", players),
                                Inline = true
                            },
                            new EmbedFieldBuilder()
                            {
                                Name = "Village count",
                                Value = string.Join("\n", villages),
                                Inline = true
                            }],
                        Color = Color.Green
                    }
                ],
            });

            var updateServerListCommand = scope.ServiceProvider.GetRequiredService<UpdateServerListCommand.Handler>();
            await updateServerListCommand.HandleAsync(new([.. serverRecords.Select(x => x.Server)]), cancellationToken);
            _hostApplicationLifetime.StopApplication();
        }

        private async Task<UpdateDatabaseCommand.Response[]> GetServerRecords((string ServerUrl, List<RawVillage> RawVillages, TimeSpan FetchingRuntime, TimeSpan ParsingRuntime)[] rawVillageRecords, UpdateDatabaseCommand.Handler updateDatabaseCommand, CancellationToken cancellationToken)
        {
            using var semaphore = new SemaphoreSlim(10);
            var tasks = rawVillageRecords.Select(record => Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var serverRecord = await updateDatabaseCommand.HandleAsync(new(record.ServerUrl, record.RawVillages), cancellationToken);
                    return serverRecord;
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken)).ToArray();
            var sw = Stopwatch.StartNew();
            var records = await Task.WhenAll(tasks);
            sw.Stop();
            _logger.LogInformation("Runtime for update map SQLs to database: {Minutes}m {Seconds}s", sw.ElapsedMilliseconds / 1000 / 60, (sw.ElapsedMilliseconds / 1000) % 60);
            return records;
        }

        private async Task<(string ServerUrl, List<RawVillage> RawVillages, TimeSpan FetchingRuntime, TimeSpan ParsingRuntime)[]> GetRawVillages(List<string> serverUrls, GetMapSqlCommand.Handler getMapSqlCommand, GetRawVillageCommand.Handler getRawVillageCommand, CancellationToken cancellationToken)
        {
            using var semaphore = new SemaphoreSlim(10);
            var tasks = serverUrls.Select(serverUrl => Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var mapSqlResponse = await getMapSqlCommand.HandleAsync(new(serverUrl), cancellationToken);
                    var rawVillageResponse = await getRawVillageCommand.HandleAsync(new(mapSqlResponse.MapSqlStream), cancellationToken);
                    return (serverUrl, rawVillageResponse.RawVillages, mapSqlResponse.Runtime, rawVillageResponse.Runtime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing map SQL for server {Url}", serverUrl);
                    return (serverUrl, [], TimeSpan.Zero, TimeSpan.Zero);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken)).ToArray();

            var sw = Stopwatch.StartNew();
            var mapSqlRecord = await Task.WhenAll(tasks);
            sw.Stop();
            _logger.LogInformation("Runtime for fetching & parsing map SQLs: {Minutes}m {Seconds}s", sw.ElapsedMilliseconds / 1000 / 60, (sw.ElapsedMilliseconds / 1000) % 60);
            return mapSqlRecord;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}