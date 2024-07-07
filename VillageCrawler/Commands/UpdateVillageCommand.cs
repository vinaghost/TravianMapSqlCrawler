using MediatR;
using Microsoft.EntityFrameworkCore;
using VillageCrawler.DbContexts;
using VillageCrawler.Entities;
using VillageCrawler.Extensions;
using VillageCrawler.Models;

namespace VillageCrawler.Commands
{
    public record UpdateVillageCommand(VillageDbContext Context, List<RawVillage> VillageRaws) : IRequest;

    public class UpdateVillageCommandHandler : IRequestHandler<UpdateVillageCommand>
    {
        public async Task Handle(UpdateVillageCommand request, CancellationToken cancellationToken)
        {
            var context = request.Context;
            var villages = request.VillageRaws
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