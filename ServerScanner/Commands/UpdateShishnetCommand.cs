using HtmlAgilityPack;
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
    public static partial class UpdateShishnetCommand
    {
        public sealed record Command();
        public const string Url = "https://travmap.shishnet.org/status.php";
        public const string AddServerUrl = "https://travmap.shishnet.org/add.php?server=";

        private static async ValueTask HandleAsync(
            Command _,
            HttpClient httpClient,
            IOptions<ConnectionStrings> connectionStringsOptions,
            ILogger<Handler> logger,
            CancellationToken cancellationToken)
        {
            var response = await httpClient.GetAsync(Url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var html = new HtmlDocument();
            html.Load(await response.Content.ReadAsStreamAsync(cancellationToken));

            var table = html.DocumentNode
                .Descendants("table")
                .FirstOrDefault();
            if (table is null) return;

            var rows = table
                .Descendants("tr")
                .Select(row => row.Descendants("td"))
                .Where(cells => cells.Count() > 3)
                .Select(cells => cells.ToList())
                .Where(cells => string.Equals(cells[2].InnerText.Trim(), "ok", StringComparison.OrdinalIgnoreCase))
                .Select(cells => cells[0].InnerText.Trim())
                .ToList();

            var connectionStrings = connectionStringsOptions.Value.Server;

            await using var context = new ServerDbContext(connectionStrings);
            var servers = await context.LobbyServers
               .Select(x => x.Url)
               .ToListAsync(cancellationToken);

            var newServers = servers
                .Where(url => !rows.Contains(url, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var newServer in newServers)
            {
                logger.LogInformation("Adding new server to shishnet: {ServerUrl}", newServer);
                await httpClient.GetAsync(AddServerUrl + newServer, cancellationToken);
                await Task.Delay(5000, cancellationToken);
            }
        }
    }
}