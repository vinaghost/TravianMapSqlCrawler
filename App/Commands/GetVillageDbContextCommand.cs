using App.DbContexts;
using App.Models;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace App.Commands
{
    [Handler]
    public static partial class GetVillageDbContextCommand
    {
        public sealed record Command(string Url);

        private static async ValueTask<VillageDbContext> HandleAsync(
            Command command,
            IOptions<ConnectionStrings> connections,
            CancellationToken cancellationToken)
        {
            var context = new VillageDbContext(connections.Value.Server, command.Url);
            await context.Database.EnsureCreatedAsync(cancellationToken);
            await context.Database.ExecuteSqlRawAsync("SET GLOBAL local_infile = true;", cancellationToken);
            return context;
        }
    }
}