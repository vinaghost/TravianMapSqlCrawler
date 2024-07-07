using MediatR;
using Microsoft.EntityFrameworkCore;
using VillageCrawler.DbContexts;
using VillageCrawler.Entities;
using VillageCrawler.Models;

namespace VillageCrawler.Commands
{
    public record UpdatePlayerCommand(VillageDbContext Context, List<RawVillage> VillageRaws) : IRequest;

    public class UpdatePlayerCommandHandler : IRequestHandler<UpdatePlayerCommand>
    {
        public async Task Handle(UpdatePlayerCommand request, CancellationToken cancellationToken)
        {
            var context = request.Context;
            var players = request.VillageRaws
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
    }
}