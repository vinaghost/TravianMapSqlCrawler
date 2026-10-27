using System;
using System.Collections.Generic;
using System.Text;

using HtmlAgilityPack;

using Immediate.Handlers.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MySqlConnector;

using ServerScanner.Configuration;

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

            var connectionStrings = connectionStringsOptions.Value.Prod;

            await using var connection = new MySqlConnection($"{connectionStrings}AllowLoadLocalInfile=true;AllowUserVariables=True;");
            await connection.OpenAsync();

            const string databaseName = "Servers";
            var databaseCommandScript = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`;";

            using (var databaseCommand = new MySqlCommand(databaseCommandScript, connection))
            {
                await databaseCommand.ExecuteNonQueryAsync();
            }
            await connection.ChangeDatabaseAsync(databaseName);

            var createTableScript = $@"CREATE TABLE IF NOT EXISTS `LobbyServers` (
                `Id` VARCHAR(255) NOT NULL DEFAULT '',
                `Name` VARCHAR(255) NOT NULL DEFAULT '',
                `Url` VARCHAR(255) NOT NULL DEFAULT '',
                `UpdateAt` DATETIME NOT NULL,
                PRIMARY KEY (`Id`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            using (var createTableCommand = new MySqlCommand(createTableScript, connection))
            {
                await createTableCommand.ExecuteNonQueryAsync();
            }

            var serverUrls = new List<string>();
            var selectScript = "SELECT `Url` FROM `LobbyServers`;";

            using (var selectCmd = new MySqlCommand(selectScript, connection))
            using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    serverUrls.Add(reader.GetString(0));
                }
            }

            var newServers = serverUrls
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