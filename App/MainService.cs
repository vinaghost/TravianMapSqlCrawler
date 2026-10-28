using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using App.Database;
using App.Entities;
using App.Models;

using ConsoleTables;

using CSharpDiscordWebhook;
using CSharpDiscordWebhook.Objects;

using HtmlAgilityPack;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using MySqlConnector;

using Serilog;

namespace App
{
    public partial class MainService : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IConfiguration _configuration;

        public MainService(IHostApplicationLifetime hostApplicationLifetime, IConfiguration configuration)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var httpClient = new HttpClient();
            var serverUrls = await GetServerUrls(httpClient);

            var rawVillageRecords = await GetRawVillages(httpClient, [serverUrls[0]]);

            ConsoleTable
               .From(rawVillageRecords.Select(r => new { r.ServerUrl, r.ExecutionTime })
                   .Append(new
                   {
                       ServerUrl = $"Total [{rawVillageRecords.Length}]",
                       ExecutionTime = rawVillageRecords.Select(x => x.ExecutionTime).Aggregate(TimeSpan.Zero, (total, next) => total.Add(next)),
                   }))
               .Configure(o => o.NumberAlignment = Alignment.Right)
               .Write(Format.Minimal);

            var serverRecords = await UpdateServerRecords(rawVillageRecords);

            ConsoleTable
               .From(serverRecords.OrderByDescending(x => x.Server.PlayerCount).Select(r => new { r.Server.Url, r.Server.VillageCount, r.Server.PlayerCount, r.Server.AllianceCount, r.Runtime })
                   .Append(new
                   {
                       Url = $"Total [{serverRecords.Length}]",
                       VillageCount = 0,
                       PlayerCount = 0,
                       AllianceCount = 0,
                       Runtime = serverRecords.Select(x => x.Runtime).Aggregate(TimeSpan.Zero, (total, next) => total.Add(next)),
                   }))
               .Configure(o => o.NumberAlignment = Alignment.Right)
               .Write(Format.Minimal);

            var serversGroupedByRegion = serverRecords
                .OrderByDescending(x => x.Server.PlayerCount)
                .GroupBy(x => x.Server.Url.Split('.').TakeLast(3).First()) // Group by region (last part of the URL)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => new
                    {
                        Server = x.Server.Url.Replace(".travian.com", ""),
                        PlayerCount = x.Server.PlayerCount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture),
                        VillageCount = x.Server.VillageCount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
                    })
                    .ToList()
                );

            var singleServerGroups = serversGroupedByRegion.Where(x => x.Value.Count == 1).SelectMany(x => x.Value).ToList();
            var multiServerGroups = serversGroupedByRegion.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);

            var embedBuilders = new List<EmbedBuilder>();

            embedBuilders.AddRange(multiServerGroups
                .OrderByDescending(x => x.Value.Count)
                .Select(regionGroup =>
                {
                    var sortedServers = regionGroup.Value.OrderByDescending(x => int.Parse(x.PlayerCount, System.Globalization.NumberStyles.AllowThousands)).ToList();
                    return new EmbedBuilder
                    {
                        Color = Color.Green,
                        Description = $"Region {regionGroup.Key} has {sortedServers.Count} servers.",
                        Fields = [
                            new EmbedFieldBuilder()
                            {
                                Name = "Server",
                                Value = string.Join("\n", sortedServers.Select(x => x.Server)),
                                Inline = true,
                            },
                            new EmbedFieldBuilder()
                            {
                                Name = "Player count",
                                Value = string.Join("\n", sortedServers.Select(x => x.PlayerCount)),
                                Inline = true
                            },
                            new EmbedFieldBuilder()
                            {
                                Name = "Village count",
                                Value = string.Join("\n", sortedServers.Select(x => x.VillageCount)),
                                Inline = true
                            }],
                    };
                }));

            if (singleServerGroups.Count > 0)
            {
                var sortedServers = singleServerGroups.OrderByDescending(x => int.Parse(x.PlayerCount, System.Globalization.NumberStyles.AllowThousands)).ToList();
                embedBuilders.Add(new EmbedBuilder
                {
                    Color = Color.Green,
                    Description = $"Region server has {sortedServers.Count} servers.",
                    Fields = [
                        new EmbedFieldBuilder()
                        {
                            Name = "Server",
                            Value = string.Join("\n", sortedServers.Select(x => x.Server)),
                            Inline = true,
                        },
                        new EmbedFieldBuilder()
                        {
                            Name = "Player count",
                            Value = string.Join("\n", sortedServers.Select(x => x.PlayerCount)),
                            Inline = true
                        },
                        new EmbedFieldBuilder()
                        {
                            Name = "Village count",
                            Value = string.Join("\n", sortedServers.Select(x => x.VillageCount)),
                            Inline = true
                        }],
                });
            }

            using var webhook = new DiscordWebhook(new Uri(_configuration["DiscordWebhookUrl"]!));
            await webhook.SendMessageAsync(new MessageBuilder
            {
                Content = $"{serverRecords.Length} servers updated at <t:{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}:f>",
                Embeds = embedBuilders,
            });

            await UpdateServerList([.. serverRecords.Select(x => x.Server)]);
            _hostApplicationLifetime.StopApplication();
        }

        private async Task<Response[]> UpdateServerRecords(RawVillageRecord[] rawVillageRecords)
        {
            var updateRecordService = new UpdateRecordService(_configuration["DatabaseConnectionString"]!);

            using var semaphore = new SemaphoreSlim(2);
            var tasks = rawVillageRecords
                .Where(x => x.RawVillages.Count > 0)
                .Select(record => Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var serverRecord = await updateRecordService.Update(record.ServerUrl, record.RawVillages);
                        return serverRecord;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }))
                .ToArray();
            var sw = Stopwatch.StartNew();
            var records = await Task.WhenAll(tasks);
            sw.Stop();
            Log.Information("Runtime for update map SQLs to database: {Minutes}m {Seconds}s", sw.ElapsedMilliseconds / 1000 / 60, (sw.ElapsedMilliseconds / 1000) % 60);
            return records;
        }

        public record struct RawVillageRecord(string ServerUrl, List<RawVillage> RawVillages, TimeSpan ExecutionTime);

        private async Task<RawVillageRecord[]> GetRawVillages(HttpClient httpClient, List<string> serverUrls)
        {
            using var semaphore = new SemaphoreSlim(10);

            var tasks = serverUrls.Select(serverUrl => Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var sw = Stopwatch.StartNew();
                    var mapStream = await GetMapSql(httpClient, serverUrl) ?? throw new Exception($"Map SQL not found for server {serverUrl}");
                    var rawVillages = await GetRawVillages(mapStream);
                    sw.Stop();
                    return new RawVillageRecord(serverUrl, rawVillages, sw.Elapsed);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while processing map SQL for server {Url}", serverUrl);
                    return default;
                }
                finally
                {
                    semaphore.Release();
                }
            })).ToArray();

            var mainSw = Stopwatch.StartNew();
            var rawVillageRecords = await Task.WhenAll(tasks);
            mainSw.Stop();
            Log.Information("Runtime for fetching & parsing map SQLs: {Minutes}m {Seconds}s", mainSw.ElapsedMilliseconds / 1000 / 60, (mainSw.ElapsedMilliseconds / 1000) % 60);
            return [.. rawVillageRecords.Where(x => x != default)];
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        public class TravianResponse
        {
            [JsonPropertyName("items")]
            public required Dictionary<string, ServerInfo> Items { get; set; }
        }

        public class ServerInfo
        {
            [JsonPropertyName("id")]
            public required string Id { get; set; }

            [JsonPropertyName("name")]
            public required string Name { get; set; }

            [JsonPropertyName("state")]
            public required int State { get; set; }

            [JsonPropertyName("url")]
            public required string Url { get; set; }
        }

        private static async Task<List<string>> GetServerUrls(HttpClient httpClient)
        {
            const string Url = "https://status.travian.com/";

            var response = await httpClient.GetAsync(Url);
            response.EnsureSuccessStatusCode();
            var html = new HtmlDocument();
            html.Load(response.Content.ReadAsStream());

            var scriptElement = html.GetElementbyId("__GW_DATA__");

            if (scriptElement is null) return [];

            var scriptContent = scriptElement.InnerText;

            var result = JsonSerializer.Deserialize<TravianResponse>(scriptContent);

            var rows = new List<string>();
            if (result?.Items is not null)
            {
                foreach (var kvp in result.Items)
                {
                    string rawUrl = kvp.Value.Url;

                    // Convert string to Uri to safely pull out the host segment
                    var uri = new Uri(rawUrl);
                    string host = uri.Host;

                    rows.Add(host);
                }
            }

            return rows;
        }

        private static async Task<StreamReader?> GetMapSql(HttpClient httpClient, string serverUrl)
        {
            const string UrlMapSqlTemplate = "https://{0}/map.sql";

            var mapUrl = string.Format(UrlMapSqlTemplate, serverUrl);

            try
            {
                var response = await httpClient.GetAsync(mapUrl);
                response.EnsureSuccessStatusCode();
                var streamReader = new StreamReader(response.Content.ReadAsStream());
                return streamReader;
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                if (ex.Message.Contains("Name or service not known"))
                {
                    return null;
                }
                throw;
            }
        }

        private static async Task<List<RawVillage>> GetRawVillages(StreamReader mapStream)
        {
            var villages = new List<RawVillage>();
            string? line;
            while ((line = await mapStream.ReadLineAsync()) is not null)
            {
                var village = GetVillage(line);
                if (village is not null)
                {
                    villages.Add(village);
                }
            }
            return villages;
        }

        private static RawVillage? GetVillage(string line)
        {
            if (string.IsNullOrEmpty(line)) return null;

            // Regex pattern to match the VALUES section of the SQL INSERT statement
            var regex = MapSqlRegex();

            var match = regex.Match(line);
            if (!match.Success) return null;

            // Extract fields from the regex match
            var mapId = int.Parse(match.Groups["mapId"].Value);
            var x = int.Parse(match.Groups["x"].Value);
            var y = int.Parse(match.Groups["y"].Value);
            var tribe = int.Parse(match.Groups["tribe"].Value);
            var villageId = int.Parse(match.Groups["villageId"].Value);
            var villageName = match.Groups["villageName"].Value;
            var playerId = int.Parse(match.Groups["playerId"].Value);
            var playerName = match.Groups["playerName"].Value;
            var allianceId = int.Parse(match.Groups["allianceId"].Value);
            var allianceName = match.Groups["allianceName"].Value;
            var population = int.Parse(match.Groups["population"].Value);
            var region = match.Groups["region"].Value == "NULL" ? string.Empty : match.Groups["region"].Value.Trim('\'');
            var isCapital = match.Groups["isCapital"].Value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            var isCity = match.Groups["isCity"].Value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            var isHarbor = match.Groups["isHarbor"].Value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            var victoryPoints = match.Groups["victoryPoints"].Value == "NULL" ? 0 : int.Parse(match.Groups["victoryPoints"].Value);

            return new RawVillage(mapId, x, y, tribe, villageId, villageName, playerId, playerName, allianceId, allianceName, population, region, isCapital, isCity, isHarbor, victoryPoints);
        }

        [GeneratedRegex(@"VALUES\s*\((?<mapId>-?\d+),(?<x>-?\d+),(?<y>-?\d+),(?<tribe>\d+),(?<villageId>\d+),'(?<villageName>[^']*)',(?<playerId>\d+),'(?<playerName>[^']*)',(?<allianceId>\d+),'(?<allianceName>[^']*)',(?<population>\d+),(?<region>NULL|'[^']*'),(?<isCapital>TRUE|FALSE),(?<isCity>NULL|TRUE|FALSE),(?<isHarbor>NULL|TRUE|FALSE),(?<victoryPoints>NULL|-?\d+)\);", RegexOptions.IgnoreCase, "en-IO")]
        private static partial Regex MapSqlRegex();

        private async Task UpdateServerList(List<Server> servers)
        {
            await using var connection = new MySqlConnection($"{_configuration["DatabaseConnectionString"]!}AllowLoadLocalInfile=true;AllowUserVariables=True;");
            await connection.OpenAsync();

            const string databaseName = "Servers";
            var databaseCommandScript = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`;";

            using (var command = new MySqlCommand(databaseCommandScript, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            await connection.ChangeDatabaseAsync(databaseName);

            var createTableScript = $@"CREATE TABLE IF NOT EXISTS `Servers` (
                `Id` INT NOT NULL,
                `Url` VARCHAR(255) NOT NULL DEFAULT '',
                `AllianceCount` INT NOT NULL,
                `PlayerCount` INT NOT NULL,
                `VillageCount` INT NOT NULL,
                `LastUpdate` DATETIME NOT NULL,
                PRIMARY KEY (`Id`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            using (var createTableCommand = new MySqlCommand(createTableScript, connection))
            {
                await createTableCommand.ExecuteNonQueryAsync();
            }

            var oldServers = new List<Server>();
            var selectServersScript = "SELECT * FROM `Servers`;";
            using (var selectCommand = new MySqlCommand(selectServersScript, connection))
            {
                using var reader = await selectCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32("Id");
                    var url = reader.GetString("Url");
                    var allianceCount = reader.GetInt32("AllianceCount");
                    var playerCount = reader.GetInt32("PlayerCount");
                    var villageCount = reader.GetInt32("VillageCount");
                    var lastUpdate = reader.GetDateTime("LastUpdate");
                    oldServers.Add(new Server
                    {
                        Id = id,
                        Url = url,
                        AllianceCount = allianceCount,
                        PlayerCount = playerCount,
                        VillageCount = villageCount,
                        LastUpdate = lastUpdate
                    });
                }
            }
            var timeoutServers = new List<string>();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var updates = new List<Server>();
                var inserts = new List<Server>();

                foreach (var server in servers)
                {
                    var oldServer = oldServers.FirstOrDefault(x => x.Url == server.Url);
                    if (oldServer != null)
                    {
                        server.Id = oldServer.Id;
                        updates.Add(server);
                    }
                    else
                    {
                        inserts.Add(server);
                    }
                }

                // ====================================================================
                // EXECUTE BATCH UPDATES
                // ====================================================================
                if (updates.Count > 0)
                {
                    using var updateCmd = new MySqlCommand(@"
                UPDATE `Servers`
                SET `AllianceCount` = @AllianceCount,
                    `PlayerCount` = @PlayerCount,
                    `VillageCount` = @VillageCount,
                    `LastUpdate` = @LastUpdate
                WHERE `Id` = @Id;", connection, transaction);

                    updateCmd.Parameters.Add("@Id", MySqlDbType.Int32);
                    updateCmd.Parameters.Add("@AllianceCount", MySqlDbType.Int32);
                    updateCmd.Parameters.Add("@PlayerCount", MySqlDbType.Int32);
                    updateCmd.Parameters.Add("@VillageCount", MySqlDbType.Int32);
                    updateCmd.Parameters.Add("@LastUpdate", MySqlDbType.DateTime);
                    updateCmd.Prepare();

                    foreach (var server in updates)
                    {
                        updateCmd.Parameters["@Id"].Value = server.Id;
                        updateCmd.Parameters["@AllianceCount"].Value = server.AllianceCount;
                        updateCmd.Parameters["@PlayerCount"].Value = server.PlayerCount;
                        updateCmd.Parameters["@VillageCount"].Value = server.VillageCount;
                        updateCmd.Parameters["@LastUpdate"].Value = server.LastUpdate;
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }

                // ====================================================================
                // EXECUTE BATCH INSERTS
                // ====================================================================
                if (inserts.Count > 0)
                {
                    using var insertCmd = new MySqlCommand(@"
                INSERT INTO `Servers` (`Id`, `Url`, `AllianceCount`, `PlayerCount`, `VillageCount`, `LastUpdate`)
                VALUES (@Id, @Url, @AllianceCount, @PlayerCount, @VillageCount, @LastUpdate);", connection, transaction);

                    insertCmd.Parameters.Add("@Id", MySqlDbType.Int32);
                    insertCmd.Parameters.Add("@Url", MySqlDbType.VarChar, 255);
                    insertCmd.Parameters.Add("@AllianceCount", MySqlDbType.Int32);
                    insertCmd.Parameters.Add("@PlayerCount", MySqlDbType.Int32);
                    insertCmd.Parameters.Add("@VillageCount", MySqlDbType.Int32);
                    insertCmd.Parameters.Add("@LastUpdate", MySqlDbType.DateTime);
                    insertCmd.Prepare();

                    foreach (var server in inserts)
                    {
                        insertCmd.Parameters["@Id"].Value = server.Id;
                        insertCmd.Parameters["@Url"].Value = server.Url;
                        insertCmd.Parameters["@AllianceCount"].Value = server.AllianceCount;
                        insertCmd.Parameters["@PlayerCount"].Value = server.PlayerCount;
                        insertCmd.Parameters["@VillageCount"].Value = server.VillageCount;
                        insertCmd.Parameters["@LastUpdate"].Value = server.LastUpdate;
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                string selectTimeoutScript = "SELECT `Url` FROM `Servers` WHERE `LastUpdate` < DATE_SUB(NOW(), INTERVAL 7 DAY);";

                using (var selectTimeoutCmd = new MySqlCommand(selectTimeoutScript, connection, transaction))
                {
                    using var timeoutReader = await selectTimeoutCmd.ExecuteReaderAsync();
                    while (await timeoutReader.ReadAsync())
                    {
                        timeoutServers.Add(timeoutReader.GetString("Url"));
                    }
                }

                // ====================================================================
                // EXECUTE AGE-OUT DELETIONS
                // ====================================================================
                string deleteScript = "DELETE FROM `Servers` WHERE `LastUpdate` < DATE_SUB(NOW(), INTERVAL 7 DAY);";

                using (var deleteCmd = new MySqlCommand(deleteScript, connection, transaction))
                {
                    int deletedCount = await deleteCmd.ExecuteNonQueryAsync();
                    if (deletedCount > 0)
                    {
                        Log.Information("Cleaned up {Count} timed-out inactive servers from index.", deletedCount);
                    }
                }

                // Commit transaction blocks safely
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Log.Error(ex, "Server sync process aborted.");
                throw;
            }

            if (timeoutServers.Count > 0)
            {
                var dropScriptBuilder = new StringBuilder();
                foreach (var server in timeoutServers)
                {
                    dropScriptBuilder.AppendLine($"DROP DATABASE IF EXISTS `{server}`;");
                }

                using var dropCmd = new MySqlCommand(dropScriptBuilder.ToString(), connection);
                await dropCmd.ExecuteNonQueryAsync();

                Log.Information("Successfully dropped {Count} databases.", timeoutServers.Count);

                using var webhook = new DiscordWebhook(new Uri(_configuration["DiscordWebhookUrl"]!));
                await webhook.SendMessageAsync(new MessageBuilder
                {
                    Embeds =
                    [
                        new EmbedBuilder
                    {
                        Title = "Server list",
                        Description = $"Deleted {timeoutServers.Count} servers",
                        Fields = [
                            new EmbedFieldBuilder()
                            {
                                Name = "Server url",
                                Value = string.Join("\n", timeoutServers),

                                Inline = true
                            }],
                        Color = Color.Green
                    }
                    ],
                });
            }
        }
    }
}