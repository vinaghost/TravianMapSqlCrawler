using App.Entities;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace App.Commands
{
    [Handler]
    public static partial class UpdateServerCommand
    {
        public sealed record Command(string Url);

        public sealed record Response(Server Server, long Time);

        private static async ValueTask<Response> HandleAsync(
           Command command,
           GetMapSqlCommand.Handler getMapSqlCommand,
           GetVillageDataCommand.Handler getVillageDataCommand,
           UpdateVillageDatabaseCommand.Handler updateVillageDatabaseCommand,
           ILogger<UpdateServerCommand.Handler> logger,
           CancellationToken cancellationToken)
        {
            var sw = new Stopwatch();
            sw.Start();
            var mapSql = await getMapSqlCommand.HandleAsync(new(command.Url), cancellationToken);
            var villages = await getVillageDataCommand.HandleAsync(new(mapSql), cancellationToken);
            var server = await updateVillageDatabaseCommand.HandleAsync(new(command.Url, villages), cancellationToken);
            sw.Stop();
            logger.LogInformation("Updated {Url} in {Time}s", command.Url, sw.ElapsedMilliseconds / 1000);
            return new Response(server, sw.ElapsedMilliseconds);
        }
    }
}