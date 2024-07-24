using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VillageCrawler.DbContexts;
using VillageCrawler.Entities;
using VillageCrawler.Models.Options;

namespace VillageCrawler.Commands
{
    public record UpdateServerListCommand(IList<Server> Servers) : IRequest;

    public class UpdateServerListCommandHandler(IOptions<ConnectionStrings> connections) : IRequestHandler<UpdateServerListCommand>
    {
        private readonly ConnectionStrings _connections = connections.Value;

        public async Task Handle(UpdateServerListCommand request, CancellationToken cancellationToken)
        {
            using var context = new ServerDbContext(_connections.Server);
            await context.Database.EnsureCreatedAsync(cancellationToken);
            await context.BulkSynchronizeAsync(request.Servers);

            var servers = await context.Servers
                .Where(x => x.LastUpdate < DateTime.Now.AddDays(-7))
                .Select(x => x.Url)
                .ToListAsync(cancellationToken);

            foreach (var server in servers)
            {
                await DeleteVillageDatabase(server, cancellationToken);
            }
        }

        private async Task DeleteVillageDatabase(string url, CancellationToken cancellationToken)
        {
            using var context = new VillageDbContext(_connections.Village, url);
            await context.Database.EnsureDeletedAsync(cancellationToken);
        }
    }
}