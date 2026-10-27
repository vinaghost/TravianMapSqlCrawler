using CSharpDiscordWebhook;
using CSharpDiscordWebhook.Objects;

using Immediate.Handlers.Shared;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using MySqlConnector;

using ServerScanner.Configuration;
using ServerScanner.Entities;
using ServerScanner.Models;

using System.Drawing;
using System.Text;

namespace ServerScanner.Commands
{
    [Handler]
    public static partial class UpdateServerCommand
    {
        public sealed record Command(IList<Server> Servers);

        private static async ValueTask HandleAsync(
            Command command,
            IOptions<ConnectionStrings> connectionStringsOptions,
            IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var servers = command.Servers;
            if (servers.Count == 0) return;
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

            using var insertCommand = new MySqlCommand();
            insertCommand.Connection = connection;

            var sqlBuilder = new StringBuilder(
                "INSERT INTO `LobbyServers` (`Id`, `Url`, `Name`, `UpdateAt`) VALUES "
            );

            var utcNow = DateTime.UtcNow;

            for (int i = 0; i < servers.Count; i++)
            {
                var server = servers[i];

                // Append the parameter placeholders for this row
                sqlBuilder.Append($"(@id_{i}, @url_{i}, @name_{i}, @updateAt_{i})");

                if (i < servers.Count - 1)
                {
                    sqlBuilder.Append(", ");
                }

                // Map and add the parameterized parameters securely
                insertCommand.Parameters.AddWithValue($"@id_{i}", server.Id);
                insertCommand.Parameters.AddWithValue($"@url_{i}", server.Url ?? "");
                insertCommand.Parameters.AddWithValue($"@name_{i}", server.Name ?? "");
                insertCommand.Parameters.AddWithValue($"@updateAt_{i}", utcNow);
            }

            insertCommand.CommandText = sqlBuilder.ToString();
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);

            var urls = servers
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Url.Replace(".travian.com", "", StringComparison.OrdinalIgnoreCase))
                .ToList();

            using var webhook = new DiscordWebhook(new Uri(configuration["DiscordWebhookUrl"]!));
            await webhook.SendMessageAsync(new MessageBuilder
            {
                Embeds =
                [
                    new EmbedBuilder
                    {
                        Title = "Scan new servers",
                        Description = $"{servers.Count} servers added at <t:{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}:f>",
                        Fields = [
                            new EmbedFieldBuilder()
                            {
                                Name = "Server",
                                Value = string.Join("\n", urls),
                                Inline = true,
                            },
                        ],
                        Color = Color.Green,
                    }
                ],
            });
        }
    }
}