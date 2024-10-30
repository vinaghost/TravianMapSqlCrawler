﻿using Microsoft.EntityFrameworkCore;
using VillageCrawlerCustom.Entities;

namespace VillageCrawlerCustom
{
    public class VillageDbContext(string connectionString, string database) : DbContext(GetOptions(connectionString, database))
    {
        public DbSet<Alliance> Alliances { get; set; }
        public DbSet<AllianceHistory> AlliancesHistory { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerHistory> PlayersHistory { get; set; }
        public DbSet<Village> Villages { get; set; }
        public DbSet<VillageHistory> VillagesHistory { get; set; }

        private static DbContextOptions<VillageDbContext> GetOptions(string connectionString, string database)
        {
            var optionsBuilder = new DbContextOptionsBuilder<VillageDbContext>();
            optionsBuilder
#if DEBUG
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
#endif
                .UseMySql($"{connectionString};Database={database}", ServerVersion.AutoDetect(connectionString));

            return optionsBuilder.Options;
        }
    }

    public static class VillageDbContextExtension
    {
        private static readonly DateTime Today = DateTime.Today;

        public static async Task UpdateAlliance(this VillageDbContext context, IList<RawVillage> rawVillages)
        {
            var alliances = rawVillages.GetAlliances();

            if (!await context.AlliancesHistory.AnyAsync(x => x.Date == EF.Constant(Today)))
            {
                var oldAlliances = await context.Alliances
                    .Select(x => new AllianceHistory
                    {
                        AllianceId = x.Id,
                        PlayerCount = x.PlayerCount,
                    })
                    .ToDictionaryAsync(x => x.AllianceId, x => x);

                var validAlliances = AllianceHistoryHandle(alliances, oldAlliances);

                // synchronize today data before insert history to prevent missing key error
                await context.BulkMergeAsync(alliances);
                await context.BulkInsertAsync(validAlliances);
            }
            else
            {
                await context.BulkMergeAsync(alliances);
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

        public static async Task UpdatePlayer(this VillageDbContext context, IList<RawVillage> rawVillages)
        {
            var players = rawVillages.GetPlayers();

            if (!await context.PlayersHistory.AnyAsync(x => x.Date == EF.Constant(Today)))
            {
                var oldPlayers = await context.Players
                    .Select(x => new PlayerHistory
                    {
                        PlayerId = x.Id,
                        AllianceId = x.AllianceId,
                        Population = x.Population,
                    })
                    .ToDictionaryAsync(x => x.PlayerId, x => x);

                var validPlayers = PlayerHistoryHandle(players, oldPlayers);

                // synchronize today data before insert history to prevent missing key error
                await context.BulkSynchronizeAsync(players);
                await context.BulkInsertAsync(validPlayers);
            }
            else
            {
                await context.BulkSynchronizeAsync(players);
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

        public static async Task UpdateVillage(this VillageDbContext context, IList<RawVillage> rawVillages)
        {
            var villages = rawVillages.GetVillages();

            if (!await context.VillagesHistory.AnyAsync(x => x.Date == EF.Constant(Today)))
            {
                var oldVillages = await context.Villages
                    .Select(x => new VillageHistory
                    {
                        VillageId = x.Id,
                        Population = x.Population,
                    })
                    .ToDictionaryAsync(x => x.VillageId, x => x);

                var validVillages = VillageHistoryHandle(villages, oldVillages);

                // synchronize today data before insert history to prevent missing key error
                await context.BulkSynchronizeAsync(villages);
                await context.BulkInsertAsync(validVillages);
            }
            else
            {
                await context.BulkSynchronizeAsync(villages);
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