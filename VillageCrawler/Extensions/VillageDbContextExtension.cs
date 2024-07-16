using Microsoft.EntityFrameworkCore;
using VillageCrawler.DbContexts;
using VillageCrawler.Entities;
using VillageCrawler.Models;

namespace VillageCrawler.Extensions
{
    public static class VillageDbContextExtension
    {
        private static readonly DateTime Today = DateTime.Today;

        public static async Task UpdateAlliance(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var alliances = rawVillages.GetAlliances();

            if (!await context.AlliancesHistory.AnyAsync(x => x.Date == EF.Constant(Today), cancellationToken))
            {
                var oldAlliances = await context.Alliances
                    .Select(x => new AllianceHistory
                    {
                        AllianceId = x.Id,
                        PlayerCount = x.PlayerCount,
                    })
                    .ToListAsync(cancellationToken);

                var validAlliances = AllianceHistoryHandle(alliances, oldAlliances);
                await context.BulkInsertAsync(validAlliances, cancellationToken);
            }

            await context.BulkMergeAsync(alliances.Values);
        }

        private static IEnumerable<AllianceHistory> AllianceHistoryHandle(Dictionary<int, Alliance> todayAlliances, IList<AllianceHistory> oldAlliances)
        {
            foreach (var oldAlliance in oldAlliances)
            {
                oldAlliance.Date = Today;

                var exist = todayAlliances.TryGetValue(oldAlliance.AllianceId, out var todayAlliance);
                if (exist && todayAlliance is not null)
                {
                    oldAlliance.ChangePlayerCount = todayAlliance.PlayerCount - oldAlliance.PlayerCount;
                }
                yield return oldAlliance;
            }
        }

        public static async Task UpdatePlayer(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var players = rawVillages.GetPlayers();

            if (!await context.PlayersHistory.AnyAsync(x => x.Date == EF.Constant(Today), cancellationToken))
            {
                var oldPlayers = await context.Players
                    .Select(x => new PlayerHistory
                    {
                        PlayerId = x.Id,
                        AllianceId = x.AllianceId,
                        Population = x.Population,
                    })
                    .ToListAsync(cancellationToken);

                var validPlayers = PlayerHistoryHandle(players, oldPlayers);

                await context.BulkInsertAsync(validPlayers, cancellationToken);
            }

            await context.BulkSynchronizeAsync(players.Values, cancellationToken);
        }

        private static IEnumerable<PlayerHistory> PlayerHistoryHandle(Dictionary<int, Player> todayPlayers, IList<PlayerHistory> oldPlayers)
        {
            foreach (var oldPlayer in oldPlayers)
            {
                oldPlayer.Date = Today;

                var exist = todayPlayers.TryGetValue(oldPlayer.PlayerId, out var todayPlayer);
                if (exist && todayPlayer is not null)
                {
                    oldPlayer.ChangeAlliance = todayPlayer.AllianceId != oldPlayer.AllianceId;
                    oldPlayer.ChangePopulation = todayPlayer.Population - oldPlayer.Population;
                }
                yield return oldPlayer;
            }
        }

        public static async Task UpdateVillage(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var villages = rawVillages.GetVillages();

            if (!await context.VillagesHistory.AnyAsync(x => x.Date == EF.Constant(Today), cancellationToken))
            {
                var oldVillages = await context.Villages
                    .Select(x => new VillageHistory
                    {
                        VillageId = x.Id,
                        Population = x.Population,
                    })
                    .ToListAsync(cancellationToken);

                var validVillages = VillageHistoryHandle(villages, oldVillages);
                await context.BulkInsertAsync(validVillages, cancellationToken);
            }

            await context.BulkSynchronizeAsync(villages.Values, cancellationToken);
        }

        private static IEnumerable<VillageHistory> VillageHistoryHandle(Dictionary<int, Village> todayVillages, IList<VillageHistory> oldVillages)
        {
            foreach (var oldVillage in oldVillages)
            {
                oldVillage.Date = Today;

                var exist = todayVillages.TryGetValue(oldVillage.VillageId, out var todayVillage);
                if (exist && todayVillage is not null)
                {
                    oldVillage.ChangePopulation = todayVillage.Population - oldVillage.Population;
                }
                yield return oldVillage;
            }
        }
    }
}