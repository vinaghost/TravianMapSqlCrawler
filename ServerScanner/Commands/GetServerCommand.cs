using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerScanner.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerScanner.Commands
{
    [Handler]
    public static partial class GetServerCommand
    {
        public sealed record Command();

        private static async ValueTask<List<string>> HandleAsync(
            Command _,
            IOptions<ConnectionStrings> connectionStringsOptions,
            CancellationToken cancellationToken)
        {
            var connectionStrings = connectionStringsOptions.Value.Server;

            await using var context = new ServerDbContext(connectionStrings);
            var servers = await context.LobbyServers
               .Select(x => x.Id)
               .ToListAsync(cancellationToken);
            return servers;
        }
    }
}