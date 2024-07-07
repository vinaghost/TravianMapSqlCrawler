using MediatR;
using Microsoft.EntityFrameworkCore;
using VillageCrawler.DbContexts;
using VillageCrawler.Entities;
using VillageCrawler.Models;

namespace VillageCrawler.Commands
{
    public record UpdateAllianceCommand(VillageDbContext Context, List<RawVillage> RawVillages) : IRequest;

    public class UpdateAllianceCommandHandler : IRequestHandler<UpdateAllianceCommand>
    {
        public async Task Handle(UpdateAllianceCommand request, CancellationToken cancellationToken)
        {
            var context = request.Context;
            var alliances = request.RawVillages
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
    }
}