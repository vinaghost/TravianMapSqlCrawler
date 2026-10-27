using Immediate.Handlers.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using MySqlConnector;

using ServerScanner.Configuration;

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

            var serverIds = new List<string>();
            var selectScript = "SELECT `Id` FROM `LobbyServers`;";

            using (var selectCmd = new MySqlCommand(selectScript, connection))
            using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    serverIds.Add(reader.GetString(0));
                }
            }
            return serverIds;
        }
    }
}