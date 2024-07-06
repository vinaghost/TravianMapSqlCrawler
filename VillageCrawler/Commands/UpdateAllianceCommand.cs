using MediatR;
using VillageCrawler.DbContexts;
using VillageCrawler.Extensions;
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
                .DistinctBy(x => x.AllianceId)
                .Select(x => x.GetAlliace());
            await context.BulkMergeAsync(alliances);
        }
    }
}