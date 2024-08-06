using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VillageCrawler.DbContexts;
using VillageCrawler.Entities;
using VillageCrawler.Models.Options;

namespace VillageCrawler.Commands
{
    public record UpdateServerListCommand(List<Server> Servers) : IRequest;

    public class UpdateServerListCommandHandler(IOptions<ConnectionStrings> connections, ILogger<UpdateServerListCommand> logger) : IRequestHandler<UpdateServerListCommand>
    {
        private readonly ConnectionStrings _connections = connections.Value;
        private readonly ILogger<UpdateServerListCommand> _logger = logger;

        public async Task Handle(UpdateServerListCommand request, CancellationToken cancellationToken)
        {
            using var context = new ServerDbContext(_connections.Server);
            await context.Database.EnsureCreatedAsync(cancellationToken);

            var servers = await context.Servers
                .ToListAsync(cancellationToken);

            foreach (var server in servers)
            {
                var newServer = request.Servers.Find(x => x.Url == server.Url);
                if (newServer is null) continue;

                server.AllianceCount = newServer.AllianceCount;
                server.PlayerCount = newServer.PlayerCount;
                server.VillageCount = newServer.VillageCount;
                server.LastUpdate = newServer.LastUpdate;
            }

            var newServers = request.Servers
                .Where(x => !servers.Exists(y => y.Url == x.Url))
                .ToList();

            await context.AddRangeAsync(newServers, cancellationToken);
            await context.BulkSaveChangesAsync(cancellationToken);

            var timeoutServers = await context.Servers
                .Where(x => x.LastUpdate < DateTime.Now.AddDays(-7))
                .Select(x => new { x.Id, x.Url })
                .ToListAsync(cancellationToken);

            if (timeoutServers.Count == 0) return;
            _logger.LogInformation("Deleting {Count} servers: {Servers}", timeoutServers.Count, timeoutServers);

            await context.Servers
                .Where(x => timeoutServers.Select(x => x.Id).Contains(x.Id))
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var server in timeoutServers)
            {
                await DeleteVillageDatabase(server.Url, cancellationToken);
            }
        }

        private async Task DeleteVillageDatabase(string url, CancellationToken cancellationToken)
        {
            using var context = new VillageDbContext(_connections.Village, url);
            await context.Database.EnsureDeletedAsync(cancellationToken);
        }
    }
}