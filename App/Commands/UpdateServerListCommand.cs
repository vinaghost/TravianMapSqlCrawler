using App.DbContexts;
using App.Entities;
using App.Models;
using EFCore.BulkExtensions;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Commands
{
    [Handler]
    public static partial class UpdateServerListCommand
    {
        public sealed record Command(List<Server> Servers);

        private static async ValueTask HandleAsync(
            Command command,
            IOptions<ConnectionStrings> connections,
            ILogger<UpdateServerListCommand.Handler> logger,
            CancellationToken cancellationToken)
        {
            var servers = command.Servers;
            using var context = new ServerDbContext(connections.Value.Server);
            await context.Database.EnsureCreatedAsync(cancellationToken);

            var dbServers = await context.Servers
                .ToListAsync(cancellationToken);

            foreach (var server in servers)
            {
                var dbServer = dbServers.FirstOrDefault(x => x.Url == server.Url);
                if (dbServer is null) continue;

                dbServer.AllianceCount = server.AllianceCount;
                dbServer.PlayerCount = server.PlayerCount;
                dbServer.VillageCount = server.VillageCount;
                dbServer.LastUpdate = server.LastUpdate;
            }

            var newServers = servers
                .Where(x => !dbServers.Exists(y => y.Url == x.Url))
                .ToList();

            context.UpdateRange(dbServers);
            await context.AddRangeAsync(newServers, cancellationToken);
            await context.BulkSaveChangesAsync(cancellationToken: cancellationToken);

            var timeoutServers = await context.Servers
                .Where(x => x.LastUpdate < DateTime.Now.AddDays(-7))
                .Select(x => new { x.Id, x.Url })
                .ToListAsync(cancellationToken);

            if (timeoutServers.Count == 0) return;
            logger.LogInformation("Deleting {Count} servers: {Servers}", timeoutServers.Count, timeoutServers);

            await context.Servers
                .Where(x => timeoutServers.Select(x => x.Id).Contains(x.Id))
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var timeoutServer in timeoutServers)
            {
                using var villageDbContext = new VillageDbContext(connections.Value.Village, timeoutServer.Url);
                await villageDbContext.Database.EnsureDeletedAsync(cancellationToken);
            }
        }
    }
}