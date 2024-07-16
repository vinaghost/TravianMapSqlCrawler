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
            var alliances = rawVillages.GetAlliances();

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
                    var exist = alliances.TryGetValue(alliance.AllianceId, out var todayAlliance);
                    if (!exist) { continue; }
                    if (todayAlliance is null) { continue; }
                    alliance.ChangePlayerCount = todayAlliance.PlayerCount - alliance.PlayerCount;
                }

                await context.BulkInsertAsync(oldAlliances, cancellationToken);
            }

            await context.BulkMergeAsync(alliances.Values);
        }

        public static async Task UpdatePlayer(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var players = rawVillages.GetPlayers();

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
                    var exist = players.TryGetValue(player.PlayerId, out var todayPlayer);
                    if (!exist) { continue; }
                    if (todayPlayer is null) { continue; }
                    player.ChangeAlliance = todayPlayer.AllianceId == player.AllianceId;
                    player.ChangePopulation = todayPlayer.Population - player.Population;
                }

                await context.BulkInsertAsync(oldPlayers, cancellationToken);
            }

            await context.BulkSynchronizeAsync(players.Values, options => options.SynchronizeKeepidentity = true, cancellationToken);
        }

        public static async Task UpdateVillage(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var villages = rawVillages.GetVillages();

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
                    var exist = villages.TryGetValue(player.VillageId, out var todayVillage);
                    if (!exist) { continue; }
                    if (todayVillage is null) { continue; }
                    player.ChangePopulation = todayVillage.Population - player.Population;
                }

                await context.BulkInsertAsync(oldVillages, cancellationToken);
            }

            await context.BulkSynchronizeAsync(villages.Values, options => options.SynchronizeKeepidentity = true, cancellationToken);
        }
    }
}