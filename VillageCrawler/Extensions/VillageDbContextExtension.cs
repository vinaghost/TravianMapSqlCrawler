using Microsoft.EntityFrameworkCore;
using VillageCrawler.DbContexts;
using VillageCrawler.Entities;
using VillageCrawler.Models;

namespace VillageCrawler.Extensions
{
    public static class VillageDbContextExtension
    {
        public static async Task UpdateAlliance(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var alliances = rawVillages
                .DistinctBy(x => x.PlayerId)
                .GroupBy(x => x.AllianceId)
                .Select(x => new Alliance
                {
                    Id = x.Key,
                    Name = x.First().AllianceName,
                    PlayerCount = x.Count(),
                })
                .ToDictionary(x => x.Id, x => x);

            var today = DateTime.Today;
            if (!await context.AlliancesHistory.AnyAsync(x => x.Date == EF.Constant(today), cancellationToken))
            {
                var oldAlliances = context.Alliances
                    .Select(x => new
                    {
                        x.Id,
                        x.PlayerCount,
                    })
                    .AsEnumerable()
                    .Select(x => new AllianceHistory
                    {
                        AllianceId = x.Id,
                        Date = today,
                        PlayerCount = x.PlayerCount,
                    })
                    .ToList();

                foreach (var alliance in oldAlliances)
                {
                    var exist = alliances.TryGetValue(alliance.Id, out var todayAlliance);
                    if (!exist) { continue; }
                    alliance.ChangePlayerCount = todayAlliance?.PlayerCount == alliance.PlayerCount;
                }

                await context.BulkInsertOptimizedAsync(oldAlliances, cancellationToken);
            }

            await context.BulkMergeAsync(alliances.Values);
        }

        public static async Task UpdatePlayer(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var players = rawVillages
               .GroupBy(x => x.PlayerId)
               .Select(x => new Player
               {
                   Id = x.Key,
                   Name = x.First().PlayerName,
                   AllianceId = x.First().AllianceId,
                   Population = x.Sum(x => x.Population),
                   VillageCount = x.Count(),
               })
               .ToDictionary(x => x.Id, x => x);

            var today = DateTime.Today;

            if (!await context.PlayersHistory.AnyAsync(x => x.Date == EF.Constant(today), cancellationToken))
            {
                var oldPlayers = context.Players
                    .Select(x => new
                    {
                        x.Id,
                        x.AllianceId,
                        x.Population,
                    })
                    .AsEnumerable()
                    .Select(x => new PlayerHistory
                    {
                        PlayerId = x.Id,
                        Date = today,
                        AllianceId = x.AllianceId,
                        Population = x.Population,
                    })
                    .ToList();

                foreach (var player in oldPlayers)
                {
                    var exist = players.TryGetValue(player.Id, out var todayPlayer);
                    if (!exist) { continue; }
                    player.ChangeAlliance = todayPlayer?.AllianceId == player.AllianceId;
                    player.ChangePopulation = todayPlayer?.Population == player.Population;
                }

                await context.BulkInsertOptimizedAsync(oldPlayers, cancellationToken);
            }

            await context.BulkSynchronizeAsync(players.Values, options => options.SynchronizeKeepidentity = true, cancellationToken);
        }

        public static async Task UpdateVillage(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var villages = rawVillages
                .Select(x => x.GetVillage())
                .ToDictionary(x => x.Id, x => x);

            var today = DateTime.Today;

            if (!await context.VillagesHistory.AnyAsync(x => x.Date == EF.Constant(today), cancellationToken))
            {
                var oldVillages = context.Villages
                    .Select(x => new
                    {
                        x.Id,
                        x.Population,
                    })
                    .AsEnumerable()
                    .Select(x => new VillageHistory
                    {
                        VillageId = x.Id,
                        Date = today,
                        Population = x.Population,
                    })
                    .ToList();

                foreach (var player in oldVillages)
                {
                    var exist = villages.TryGetValue(player.Id, out var todayVillage);
                    if (!exist) { continue; }
                    player.ChangePopulation = todayVillage?.Population == player.Population;
                }

                await context.BulkInsertOptimizedAsync(oldVillages, cancellationToken);
            }

            await context.BulkSynchronizeAsync(villages.Values, options => options.SynchronizeKeepidentity = true, cancellationToken);
        }
    }
}