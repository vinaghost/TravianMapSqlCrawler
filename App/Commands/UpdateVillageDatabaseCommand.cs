using App.DbContexts;
using App.Entities;
using App.Models;
using EFCore.BulkExtensions;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Commands
{
    [Handler]
    public static partial class UpdateVillageDatabaseCommand
    {
        public sealed record Command(string Url, List<RawVillage> Villages);

        private static async ValueTask<Server> HandleAsync(
            Command command,
            GetVillageDbContextCommand.Handler getVillageDbContextCommand,
            ILogger<UpdateVillageDatabaseCommand.Handler> logger,
            CancellationToken cancellationToken)
        {
            var (url, villages) = command;
            using var context = await getVillageDbContextCommand.HandleAsync(new(url), cancellationToken);
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
                logger.LogError(e, "{Message}", e.Message);
                await transaction.RollbackAsync(cancellationToken);
            }

            var allianceCount = await context.Alliances.CountAsync(cancellationToken: cancellationToken);
            var playerCount = await context.Players.CountAsync(cancellationToken: cancellationToken);
            var villageCount = await context.Villages.CountAsync(cancellationToken: cancellationToken);
            var now = DateTime.Now;
            return new Server()
            {
                Url = url,
                AllianceCount = allianceCount,
                PlayerCount = playerCount,
                VillageCount = villageCount,
                LastUpdate = now
            };
        }

        private static readonly DateTime Today = DateTime.Today;

        public static List<Alliance> GetAlliances(this IList<RawVillage> rawVillages)
        {
            return rawVillages
                .DistinctBy(x => x.PlayerId)
                .GroupBy(x => x.AllianceId)
                .Select(x => new Alliance
                {
                    Id = x.Key,
                    Name = x.First().AllianceName,
                    PlayerCount = x.Count(),
                })
                .ToList();
        }

        public static List<Player> GetPlayers(this IList<RawVillage> rawVillages)
        {
            return rawVillages
               .GroupBy(x => x.PlayerId)
               .Select(x => new Player
               {
                   Id = x.Key,
                   Name = x.First().PlayerName,
                   AllianceId = x.First().AllianceId,
                   Population = x.Sum(x => x.Population),
                   VillageCount = x.Count(),
               })
               .ToList();
        }

        private static List<Village> GetVillages(this IList<RawVillage> rawVillages)
        {
            return rawVillages
                .Select(x => x.GetVillage())
                .ToList();
        }

        private static Village GetVillage(this RawVillage rawVillage)
        {
            return new Village
            {
                Id = rawVillage.VillageId,
                MapId = rawVillage.MapId,
                Name = rawVillage.VillageName,
                Tribe = rawVillage.Tribe,
                X = rawVillage.X,
                Y = rawVillage.Y,
                PlayerId = rawVillage.PlayerId,
                IsCapital = rawVillage.IsCapital,
                IsCity = rawVillage.IsCity,
                IsHarbor = rawVillage.IsHarbor,
                Population = rawVillage.Population,
                Region = rawVillage.Region,
                VictoryPoints = rawVillage.VictoryPoints
            };
        }

        private static async Task UpdateAlliance(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
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
                    .ToDictionaryAsync(x => x.AllianceId, x => x, cancellationToken);

                var validAlliances = AllianceHistoryHandle(alliances, oldAlliances);

                // synchronize today data before insert history to prevent missing key error
                await context.BulkInsertOrUpdateAsync(alliances, cancellationToken: cancellationToken);
                await context.BulkInsertAsync(validAlliances, cancellationToken: cancellationToken);
            }
            else
            {
                await context.BulkInsertOrUpdateAsync(alliances);
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

        private static async Task UpdatePlayer(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
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
                    .ToDictionaryAsync(x => x.PlayerId, x => x, cancellationToken);

                var validPlayers = PlayerHistoryHandle(players, oldPlayers);

                // synchronize today data before insert history to prevent missing key error
                await context.BulkInsertOrUpdateAsync(players, cancellationToken: cancellationToken);
                await context.BulkInsertAsync(validPlayers, cancellationToken: cancellationToken);
            }
            else
            {
                await context.BulkInsertOrUpdateAsync(players, cancellationToken: cancellationToken);
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

        private static async Task UpdateVillage(this VillageDbContext context, IList<RawVillage> rawVillages, CancellationToken cancellationToken)
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
                    .ToDictionaryAsync(x => x.VillageId, x => x, cancellationToken);

                var validVillages = VillageHistoryHandle(villages, oldVillages);

                // synchronize today data before insert history to prevent missing key error
                await context.BulkInsertOrUpdateAsync(villages, cancellationToken: cancellationToken);
                await context.BulkInsertAsync(validVillages, cancellationToken: cancellationToken);
            }
            else
            {
                await context.BulkInsertOrUpdateAsync(villages, cancellationToken: cancellationToken);
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