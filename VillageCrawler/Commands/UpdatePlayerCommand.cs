using VillageCrawler.Entities;
using VillageCrawler.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VillageCrawler.DbContexts;
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
               });

            await context.BulkSynchronizeAsync(players, options => options.SynchronizeKeepidentity = true, cancellationToken);

            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            if (!await context.PlayerAllianceHistory.AnyAsync(x => x.Date == today, cancellationToken))
            {
                var allianceHistory = await context.PlayerAllianceHistory
                    .Where(x => x.Date == yesterday)
                    .Select(x => new
                    {
                        x.PlayerId,
                        x.AllianceId
                    })
                    .OrderBy(x => x.PlayerId)
                    .ToListAsync(cancellationToken);
                var alliances = players
                    .Select(x => x.GetPlayerAlliance(today));

                foreach (var alliance in alliances)
                {
                    var history = allianceHistory.Find(x => x.PlayerId == alliance.PlayerId);
                    if (history is null) { continue; }
                    if (history.AllianceId == alliance.AllianceId)
                    {
                        alliance.Change = 0;
                    }
                    else
                    {
                        alliance.Change = 1;
                    }
                }

                await context.BulkInsertAsync(alliances, cancellationToken);
            }

            if (!await context.PlayerPopulationHistory.AnyAsync(x => x.Date == today, cancellationToken))
            {
                var populationHistory = await context.PlayerPopulationHistory
                    .Where(x => x.Date == yesterday)
                    .Select(x => new
                    {
                        x.PlayerId,
                        x.Population
                    })
                    .OrderBy(x => x.PlayerId)
                    .ToListAsync(cancellationToken);

                var populations = players
                    .Select(x => x.GetPlayerPopulation(DateTime.Today))
                    .ToList();

                foreach (var population in populations)
                {
                    var history = populationHistory.Find(x => x.PlayerId == population.PlayerId);
                    if (history is null) { continue; }
                    population.Change = population.Population - history.Population;
                }

                await context.BulkInsertAsync(populations, cancellationToken);
            }
        }
    }
}