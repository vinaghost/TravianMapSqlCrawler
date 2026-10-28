using System.Diagnostics;
using System.Reflection;
using System.Text;

using App.Entities;

using MySqlConnector;

using Serilog;

namespace App
{
    public sealed record Response(Server Server, TimeSpan Runtime);

    public class UpdateRecordService(string connectionString)
    {
        public async Task<Response> Update(string databaseName, List<RawVillage> rawVillages)
        {
            var sw = Stopwatch.StartNew();

            await using var connection = await CreateDatabaseIfNotExsits(databaseName);

            var villages = Villages(rawVillages);
            var players = Players(rawVillages);
            var alliances = Alliances(rawVillages);

            // Unique runtime identifier for the staging session tables
            string sessionSuffix = Guid.NewGuid().ToString("N")[..8];
            string stgAlliances = $"stg_alliances_{sessionSuffix}";
            string stgPlayers = $"stg_players_{sessionSuffix}";
            string stgVillages = $"stg_villages_{sessionSuffix}";

            // ====================================================================
            // STEP 1: CREATE MEMORY-BACKED TEMPORARY STAGING TABLES
            // ====================================================================
            using (var cmd = new MySqlCommand($@"
                CREATE TEMPORARY TABLE {stgAlliances} LIKE `Alliances`;
                CREATE TEMPORARY TABLE {stgPlayers} LIKE `Players`;
                CREATE TEMPORARY TABLE {stgVillages} LIKE `Villages`;", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // ====================================================================
            // STEP 2: BUILD STRING STREAM DATA BUFFERS
            // ====================================================================
            using var allianceStream = new MemoryStream();
            await using (var allianceWriter = new StreamWriter(allianceStream, Encoding.UTF8, leaveOpen: true))
            {
                foreach (var a in alliances)
                {
                    await allianceWriter.WriteLineAsync($"{a.Id}\t{EscapeCsv(a.Name)}\t{a.PlayerCount}");
                }

                await allianceWriter.FlushAsync();
            }

            using var playerStream = new MemoryStream();
            await using (var playerWriter = new StreamWriter(playerStream, Encoding.UTF8, leaveOpen: true))
            {
                foreach (var p in players)
                {
                    await playerWriter.WriteLineAsync($"{p.Id}\t{p.AllianceId}\t{EscapeCsv(p.Name)}\t{p.Population}\t{p.VillageCount}\t0"); // Always 0 initially in file dump
                }
                await playerWriter.FlushAsync();
            }

            using var villageStream = new MemoryStream();
            await using (var villageWriter = new StreamWriter(villageStream, Encoding.UTF8, leaveOpen: true))
            {
                foreach (var v in villages)
                {
                    string prevPlayer = v.PreviousPlayerId.HasValue ? v.PreviousPlayerId.Value.ToString() : "\\N";
                    await villageWriter.WriteLineAsync($"{v.Id}\t{v.MapId}\t{v.PlayerId}\t{prevPlayer}\t{EscapeCsv(v.Name)}\t{v.X}\t{v.Y}\t{v.Tribe}\t{v.Population}\t{EscapeCsv(v.Region)}\t{ToString(v.IsCapital)}\t{ToString(v.IsCity)}\t{ToString(v.IsHarbor)}\t{v.VictoryPoints}\t0");
                }
            }

            // ====================================================================
            // STEP 3: NATIVE STREAM BULK LOAD INTO STAGING TABLES
            // ====================================================================
            await StreamLocalInfileAsync(connection, stgAlliances, allianceStream, ["Id", "Name", "PlayerCount"]);
            await StreamLocalInfileAsync(connection, stgPlayers, playerStream, ["Id", "AllianceId", "Name", "Population", "VillageCount", "IsDeleted"]);
            await StreamLocalInfileAsync(connection, stgVillages, villageStream, ["Id", "MapId", "PlayerId", "PreviousPlayerId", "Name", "X", "Y", "Tribe", "Population", "Region", "IsCapital", "IsCity", "IsHarbor", "VictoryPoints", "IsDestroyed"]);

            // ====================================================================
            // STEP 4: UPSERT INTO MAIN TABLES & AUTO-FLAG MISSING RECORDS
            // ====================================================================
            string processQueries = $@"
                -- A. Upsert Alliances (No modifications to deletion rules)
                INSERT INTO `Alliances` (`Id`, `Name`, `PlayerCount`)
                SELECT `Id`, `Name`, `PlayerCount` FROM {stgAlliances}
                ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name`), `PlayerCount` = VALUES(`PlayerCount`);

                UPDATE `Alliances` a
                LEFT JOIN {stgAlliances} s ON a.`Id` = s.`Id`
                SET a.`PlayerCount` = 0
                WHERE s.`Id` IS NULL;

                -- B. Upsert Players (Reset active entities to IsDeleted=0)
                INSERT INTO `Players` (`Id`, `AllianceId`, `Name`, `Population`, `VillageCount`, `IsDeleted`)
                SELECT `Id`, `AllianceId`, `Name`, `Population`, `VillageCount`, 0 FROM {stgPlayers}
                ON DUPLICATE KEY UPDATE `AllianceId` = VALUES(`AllianceId`), `Name` = VALUES(`Name`),
                                        `Population` = VALUES(`Population`), `VillageCount` = VALUES(`VillageCount`), `IsDeleted` = 0;

                -- C. Auto soft-delete players missing from today's dump file
                UPDATE `Players` p
                LEFT JOIN {stgPlayers} s ON p.`Id` = s.`Id`
                SET p.`IsDeleted` = 1
                WHERE s.`Id` IS NULL;

                -- D. Upsert Villages with Chieftain (Conquer) Tracking Logic
                INSERT INTO `Villages` (
                    `Id`, `MapId`, `PlayerId`, `PreviousPlayerId`, `Name`, `X`, `Y`, `Tribe`,
                    `Population`, `Region`, `IsCapital`, `IsCity`, `IsHarbor`, `VictoryPoints`, `IsDestroyed`
                )
                SELECT
                    `Id`, `MapId`, `PlayerId`, `PreviousPlayerId`, `Name`, `X`, `Y`, `Tribe`,
                    `Population`, `Region`, `IsCapital`, `IsCity`, `IsHarbor`, `VictoryPoints`, 0
                FROM {stgVillages}
                ON DUPLICATE KEY UPDATE
                    -- 1. Check if the incoming PlayerId is different from what we already have in the database
                    `PreviousPlayerId` = IF(`Villages`.`PlayerId` <> VALUES(`PlayerId`), `Villages`.`PlayerId`, `Villages`.`PreviousPlayerId`),

                    -- 2. Update the current owner to the new PlayerId
                    `PlayerId` = VALUES(`PlayerId`),

                    -- 3. Update the rest of the fields normally
                    `Name` = VALUES(`Name`),
                    `Population` = VALUES(`Population`),
                    `IsCapital` = VALUES(`IsCapital`),
                    `IsCity` = VALUES(`IsCity`),
                    `IsHarbor` = VALUES(`IsHarbor`),
                    `VictoryPoints` = VALUES(`VictoryPoints`),
                    `IsDestroyed` = 0;

                -- E. Auto soft-delete villages missing from today's dump file
                UPDATE `Villages` v
                LEFT JOIN {stgVillages} s ON v.`Id` = s.`Id`
                SET v.`IsDestroyed` = 1
                WHERE s.`Id` IS NULL;

                SET @today = CURRENT_DATE();
                SET @yesterday = DATE_SUB(@today, INTERVAL 1 DAY);
                SET @already_run = (SELECT 1 FROM `PlayersHistory` WHERE `SnapshotDate` = @today LIMIT 1);

                -- Snapshot Players
                INSERT INTO `PlayersHistory` (`PlayerId`, `SnapshotDate`, `AllianceId`, `ChangeAlliances`, `Population`, `ChangePopulation`)
                SELECT p.`Id`, @today, p.`AllianceId`, IF(p.`AllianceId` <> IFNULL(ph_old.`AllianceId`, p.`AllianceId`), 1, 0), p.`Population`, (p.`Population` - IFNULL(ph_old.`Population`, p.`Population`))
                FROM `Players` p
                LEFT JOIN `PlayersHistory` ph_old ON ph_old.`PlayerId` = p.`Id` AND ph_old.`SnapshotDate` = @yesterday
                WHERE @already_run IS NULL
                ON DUPLICATE KEY UPDATE `AllianceId` = VALUES(`AllianceId`), `ChangeAlliances` = VALUES(`ChangeAlliances`), `Population` = VALUES(`Population`), `ChangePopulation` = VALUES(`ChangePopulation`);

                -- Snapshot Villages
                INSERT INTO `VillagesHistory` (`VillagesId`, `MapId`, `PlayerId`, `SnapshotDate`, `Population`, `ChangePopulation`)
                SELECT v.`Id`, v.`MapId`, v.`PlayerId`, @today, v.`Population`, (v.`Population` - IFNULL(vh_old.`Population`, v.`Population`))
                FROM `Villages` v
                LEFT JOIN `VillagesHistory` vh_old ON vh_old.`VillagesId` = v.`Id` AND vh_old.`SnapshotDate` = @yesterday
                WHERE @already_run IS NULL
                ON DUPLICATE KEY UPDATE `PlayerId` = VALUES(`PlayerId`), `Population` = VALUES(`Population`), `ChangePopulation` = VALUES(`ChangePopulation`);

                -- Snapshot Alliances
                INSERT INTO `AlliancesHistory` (`AllianceId`, `SnapshotDate`, `PlayerCount`, `ChangePlayerCount`)
                SELECT a.`Id`, @today, a.`PlayerCount`, (a.`PlayerCount` - IFNULL(ah_old.`PlayerCount`, a.`PlayerCount`))
                FROM `Alliances` a
                LEFT JOIN `AlliancesHistory` ah_old ON ah_old.`AllianceId` = a.`Id` AND ah_old.`SnapshotDate` = @yesterday
                WHERE @already_run IS NULL
                ON DUPLICATE KEY UPDATE `PlayerCount` = VALUES(`PlayerCount`), `ChangePlayerCount` = VALUES(`ChangePlayerCount`);
            ";

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                using (var processCmd = new MySqlCommand(processQueries, connection, transaction))
                {
                    processCmd.CommandTimeout = 300; // 5-minute threshold safety margin for large worlds
                    await processCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Log.Error(ex, "An error occurred during the database update process.");
            }

            var server = new Server()
            {
                Url = databaseName,
            };

            var allianceCountQuery = "SELECT COUNT(1) FROM `Alliances` WHERE PlayerCount > 0;";
            using (var allianceCountCmd = new MySqlCommand(allianceCountQuery, connection))
            {
                var allianceCount = Convert.ToInt32(await allianceCountCmd.ExecuteScalarAsync());
                server.AllianceCount = allianceCount;
            }

            var playerCountQuery = "SELECT COUNT(1) FROM `Players` WHERE IsDeleted = 0;";
            using (var playerCountCmd = new MySqlCommand(playerCountQuery, connection))
            {
                var playerCount = Convert.ToInt32(await playerCountCmd.ExecuteScalarAsync());
                server.PlayerCount = playerCount;
            }

            var villageCountQuery = "SELECT COUNT(1) FROM `Villages` WHERE IsDestroyed = 0;";
            using (var villageCountCmd = new MySqlCommand(villageCountQuery, connection))
            {
                var villageCount = Convert.ToInt32(await villageCountCmd.ExecuteScalarAsync());
                server.VillageCount = villageCount;
            }
            sw.Stop();
            return new Response(server, sw.Elapsed);
        }

        private async Task<MySqlConnection> CreateDatabaseIfNotExsits(string databaseName)
        {
            var connection = new MySqlConnection($"{connectionString}AllowLoadLocalInfile=true;AllowUserVariables=True;");
            await connection.OpenAsync();

            var databaseCommandScript = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`;";

            using (var databaseCommand = new MySqlCommand(databaseCommandScript, connection))
            {
                await databaseCommand.ExecuteNonQueryAsync();
            }

            await connection.ChangeDatabaseAsync(databaseName);

            var createTableScript = GetSqlCreateTableScript();

            using (var createTableCommand = new MySqlCommand(createTableScript, connection))
            {
                await createTableCommand.ExecuteNonQueryAsync();
            }

            return connection;
        }

        private static string GetSqlCreateTableScript()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "App.Scripts.CreateTables.sql";

            using Stream stream = (assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Could not find the embedded resource: {resourceName}")) ?? throw new FileNotFoundException($"Could not find the embedded resource: {resourceName}");
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        private static async Task StreamLocalInfileAsync(MySqlConnection connection, string table, MemoryStream memoryStream, string[] columns)
        {
            memoryStream.Position = 0;
            var loader = new MySqlBulkLoader(connection)
            {
                TableName = table,
                FieldTerminator = "\t",
                LineTerminator = "\n",
                SourceStream = memoryStream,
            };

            loader.Columns.AddRange(columns);
            await loader.LoadAsync();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\t", " ").Replace("\n", " ").Replace("\r", " ");
        }

        private static string ToString(bool value) => value ? "1" : "0";

        private static List<Alliance> Alliances(List<RawVillage> rawVillages)
        {
            var alliances = rawVillages
                .DistinctBy(x => x.PlayerId)
                .GroupBy(x => x.AllianceId)
                .Select(x => new Alliance
                {
                    Id = x.Key,
                    Name = x.Select(x => x.AllianceName).First(),
                    PlayerCount = x.Count(),
                })
                .ToList();
            return alliances;
        }

        private static List<Player> Players(List<RawVillage> rawVillages)
        {
            var players = rawVillages
                .GroupBy(x => x.PlayerId)
                .Select(x => new Player
                {
                    Id = x.Key,
                    Name = x.Select(x => x.PlayerName).First(),
                    AllianceId = x.Select(x => x.AllianceId).First(),
                    Population = x.Sum(x => x.Population),
                    VillageCount = x.Count(),
                })
                .ToList();
            return players;
        }

        private static List<Village> Villages(List<RawVillage> rawVillages)
        {
            var villages = rawVillages
                .Select(x => new Village
                {
                    Id = x.VillageId,
                    MapId = x.MapId,
                    Name = x.VillageName,
                    Tribe = x.Tribe,
                    X = x.X,
                    Y = x.Y,
                    PlayerId = x.PlayerId,
                    IsCapital = x.IsCapital,
                    IsCity = x.IsCity,
                    IsHarbor = x.IsHarbor,
                    Population = x.Population,
                    Region = x.Region,
                    VictoryPoints = x.VictoryPoints
                })
                .ToList();
            return villages;
        }
    }
}