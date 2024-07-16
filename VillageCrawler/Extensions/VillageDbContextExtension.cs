using Microsoft.EntityFrameworkCore;
using VillageCrawler.DbContexts;
using VillageCrawler.Entities;
using VillageCrawler.Models;

namespace VillageCrawler.Extensions
{
    public static class VillageDbContextExtension
    {
        private static readonly DateTime Today = DateTime.Today;
        private static readonly DateTime Yesterday = Today.AddDays(-1);

        public static async Task UpdateAlliance(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var alliances = rawVillages.GetAlliances();

            await context.BulkMergeAsync(alliances);

            if (!await context.AlliancesHistory.AnyAsync(x => x.Date == EF.Constant(Today), cancellationToken))
            {
                var oldAlliances = await context.AlliancesHistory
                    .Where(x => x.Date == EF.Constant(Yesterday))
                    .Select(x => new AllianceHistory
                    {
                        AllianceId = x.Id,
                        PlayerCount = x.PlayerCount,
                    })
                    .ToDictionaryAsync(x => x.AllianceId, x => x, cancellationToken);

                var validAlliances = AllianceHistoryHandle(alliances, oldAlliances);
                await context.BulkInsertAsync(validAlliances, cancellationToken);
            }
        }

        private static IEnumerable<AllianceHistory> AllianceHistoryHandle(IList<Alliance> todayAlliances, Dictionary<int, AllianceHistory> yesterdayAlliances)
        {
            foreach (var todayAlliance in todayAlliances)
            {
                var history = new AllianceHistory()
                {
                    AllianceId = todayAlliance.Id,
                    Date = Today,
                    PlayerCount = todayAlliance.PlayerCount,
                };

                var exist = yesterdayAlliances.TryGetValue(todayAlliance.Id, out var yesterdayAlliance);
                if (exist && yesterdayAlliance is not null)
                {
                    history.ChangePlayerCount = todayAlliance.PlayerCount - yesterdayAlliance.PlayerCount;
                }
                yield return history;
            }
        }

        public static async Task UpdatePlayer(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var players = rawVillages.GetPlayers();

            await context.BulkSynchronizeAsync(players, cancellationToken);

            if (!await context.PlayersHistory.AnyAsync(x => x.Date == EF.Constant(Today), cancellationToken))
            {
                var oldPlayers = await context.PlayersHistory
                    .Where(x => x.Date == EF.Constant(Yesterday))
                    .Select(x => new PlayerHistory
                    {
                        PlayerId = x.Id,
                        AllianceId = x.AllianceId,
                        Population = x.Population,
                    })
                    .ToDictionaryAsync(x => x.PlayerId, x => x, cancellationToken);

                var validPlayers = PlayerHistoryHandle(players, oldPlayers);

                await context.BulkInsertAsync(validPlayers, cancellationToken);
            }
        }

        private static IEnumerable<PlayerHistory> PlayerHistoryHandle(IList<Player> todayPlayers, Dictionary<int, PlayerHistory> yesterdayPlayers)
        {
            foreach (var todayPlayer in todayPlayers)
            {
                var history = new PlayerHistory()
                {
                    PlayerId = todayPlayer.Id,
                    Date = Today,
                    AllianceId = todayPlayer.AllianceId,
                    Population = todayPlayer.Population,
                };

                var exist = yesterdayPlayers.TryGetValue(todayPlayer.Id, out var yesterdayPlayer);
                if (exist && yesterdayPlayer is not null)
                {
                    history.ChangeAlliance = todayPlayer.AllianceId != yesterdayPlayer.AllianceId;
                    history.ChangePopulation = todayPlayer.Population - yesterdayPlayer.Population;
                }
                yield return history;
            }
        }

        public static async Task UpdateVillage(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
        {
            var villages = rawVillages.GetVillages();

            await context.BulkSynchronizeAsync(villages, cancellationToken);

            if (!await context.VillagesHistory.AnyAsync(x => x.Date == EF.Constant(Today), cancellationToken))
            {
                var oldVillages = await context.VillagesHistory
                    .Where(x => x.Date == EF.Constant(Yesterday))
                    .Select(x => new VillageHistory
                    {
                        VillageId = x.Id,
                        Population = x.Population,
                    })
                    .ToDictionaryAsync(x => x.VillageId, x => x, cancellationToken);

                var validVillages = VillageHistoryHandle(villages, oldVillages);
                await context.BulkInsertAsync(validVillages, cancellationToken);
            }
        }

        private static IEnumerable<VillageHistory> VillageHistoryHandle(IList<Village> todayVillages, Dictionary<int, VillageHistory> yesterdayVillages)
        {
            foreach (var todayVillage in todayVillages)
            {
                var history = new VillageHistory()
                {
                    VillageId = todayVillage.Id,
                    Date = Today,
                    Population = todayVillage.Population,
                };

                var exist = yesterdayVillages.TryGetValue(todayVillage.Id, out var yesterdayVillage);
                if (exist && yesterdayVillage is not null)
                {
                    history.ChangePopulation = todayVillage.Population - yesterdayVillage.Population;
                }
                yield return history;
            }
        }
    }
}