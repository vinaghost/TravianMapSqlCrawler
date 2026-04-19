using App.DbContexts;
using App.Entities;
using App.Models;
using EFCore.BulkExtensions;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace App.Commands
{
    [Handler]
    public static partial class UpdateDatabaseCommand
    {
        public sealed record Command(string Url, List<RawVillage> RawVillages);
        public sealed record Response(Server Server, TimeSpan Runtime);

        private static async ValueTask<Response> HandleAsync(
            Command command,
            GetVillageDbContextCommand.Handler getVillageDbContextCommand,
            ILogger<Handler> logger,
            CancellationToken cancellationToken)
        {
            var (url, rawVillages) = command;
            var sw = Stopwatch.StartNew();
            using var context = await getVillageDbContextCommand.HandleAsync(new(url), cancellationToken);

            (Dictionary<int, VillageHistory> villageOldData, bool villageHistoryLogged) = await GetVillageOldData(context, cancellationToken);
            var villages = DataManipulation.Villages(rawVillages);
            var (newVillages, oldVillages, deletedVillages, historyRecords) = DataManipulation.VillageHistory(villages, villageOldData);

            (Dictionary<int, PlayerHistory> playerOldData, bool playerHistoryLogged) = await GetPlayerOldData(context, cancellationToken);
            var players = DataManipulation.Players(rawVillages);
            var (newPlayers, oldPlayers, deletedPlayers, playerHistoryRecords) = DataManipulation.PlayerHistory(players, playerOldData);

            (Dictionary<int, AllianceHistory> allianceOldData, bool allianceHistoryLogged) = await GetAllianceOldData(context, cancellationToken);
            var alliances = DataManipulation.Alliances(rawVillages);
            var (newAlliances, oldAlliances, deletedAlliances, allianceHistoryRecords) = DataManipulation.AllianceHistory(alliances, allianceOldData);

            if (villageHistoryLogged && playerHistoryLogged && allianceHistoryLogged)
            {
                logger.LogInformation("Server {ServerUrl}'s history already logged for today.", url);
            }
            else
            {
                var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (!allianceHistoryLogged)
                    {
                        await context.BulkInsertAsync(newAlliances, cancellationToken: cancellationToken);
                        await context.BulkUpdateAsync(oldAlliances, cancellationToken: cancellationToken);
                        await context.BulkInsertAsync(allianceHistoryRecords, cancellationToken: cancellationToken);
                        await context.Alliances
                            .Where(x => deletedAlliances.Contains(x.Id))
                            .ExecuteUpdateAsync(x =>
                                x.SetProperty(a => a.PlayerCount, 0),
                            cancellationToken);
                    }

                    if (!playerHistoryLogged)
                    {
                        await context.BulkInsertAsync(newPlayers, cancellationToken: cancellationToken);
                        await context.BulkUpdateAsync(oldPlayers, cancellationToken: cancellationToken);
                        await context.BulkInsertAsync(playerHistoryRecords, cancellationToken: cancellationToken);
                        await context.Players
                            .Where(x => deletedPlayers.Contains(x.Id))
                            .ExecuteUpdateAsync(x =>
                                x.SetProperty(p => p.Population, 0)
                                 .SetProperty(p => p.AllianceId, 0)
                                 .SetProperty(p => p.VillageCount, 0),
                            cancellationToken);
                    }

                    if (!villageHistoryLogged)
                    {
                        await context.BulkInsertAsync(newVillages, cancellationToken: cancellationToken);
                        await context.BulkUpdateAsync(oldVillages, cancellationToken: cancellationToken);
                        await context.BulkInsertAsync(historyRecords, cancellationToken: cancellationToken);

                        await context.Villages
                            .Where(x => deletedVillages.Contains(x.Id))
                            .ExecuteUpdateAsync(x =>
                                x.SetProperty(v => v.Population, 0),
                                cancellationToken);
                    }

                    await transaction.CommitAsync(cancellationToken);
                    logger.LogInformation("Server {ServerUrl}'s database updated successfully.", url);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An error occurred while updating server {ServerUrl}'s database. Transaction rolled back. Error: {Message}", url, e.Message);
                    await transaction.RollbackAsync(cancellationToken);
                    return new Response(new Server()
                    {
                        Url = url,
                        AllianceCount = 0,
                        PlayerCount = 0,
                        VillageCount = 0,
                        LastUpdate = DateTime.Now
                    }, TimeSpan.Zero);
                }
            }

            var allianceCount = await context.Alliances.CountAsync(cancellationToken);
            var playerCount = await context.Players.CountAsync(cancellationToken);
            var villageCount = await context.Villages.CountAsync(cancellationToken);
            var now = DateTime.Now;
            sw.Stop();
            return new Response(new Server()
            {
                Url = url,
                AllianceCount = allianceCount,
                PlayerCount = playerCount,
                VillageCount = villageCount,
                LastUpdate = now
            }, sw.Elapsed);
        }

        private static async Task<(Dictionary<int, VillageHistory> VillageOldData, bool VillageHistoryLogged)> GetVillageOldData(VillageDbContext context, CancellationToken cancellationToken)
        {
            var villageOldData = await context.Villages
                .Select(x => new VillageHistory
                {
                    VillageId = x.Id,
                    PlayerId = x.PlayerId,
                    Population = x.Population,
                })
                .ToDictionaryAsync(x => x.VillageId, x => x, cancellationToken);
            var villageHistoryLogged = await context.VillagesHistory.AnyAsync(x => x.Date == DateTime.Today, cancellationToken);
            return (villageOldData, villageHistoryLogged);
        }

        private static async Task<(Dictionary<int, PlayerHistory> PlayerOldData, bool PlayerHistoryLogged)> GetPlayerOldData(VillageDbContext context, CancellationToken cancellationToken)
        {
            var playerOldData = await context.Players
                .Select(x => new PlayerHistory
                {
                    PlayerId = x.Id,
                    AllianceId = x.AllianceId,
                    Population = x.Population,
                })
                .ToDictionaryAsync(x => x.PlayerId, x => x, cancellationToken);
            var playerHistoryLogged = await context.PlayersHistory.AnyAsync(x => x.Date == DateTime.Today, cancellationToken);
            return (playerOldData, playerHistoryLogged);
        }

        private static async Task<(Dictionary<int, AllianceHistory> AllianceOldData, bool AllianceHistoryLogged)> GetAllianceOldData(VillageDbContext context, CancellationToken cancellationToken)
        {
            var allianceOldData = await context.Alliances
               .Select(x => new AllianceHistory
               {
                   AllianceId = x.Id,
                   PlayerCount = x.PlayerCount,
               })
               .ToDictionaryAsync(x => x.AllianceId, x => x, cancellationToken);

            var allianceHistoryLogged = await context.AlliancesHistory.AnyAsync(x => x.Date == DateTime.Today, cancellationToken);
            return (allianceOldData, allianceHistoryLogged);
        }
    }
}