using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

using App.Entities;
using App.Models;

using Microsoft.Extensions.Logging;

using MySqlConnector;

using Serilog;

namespace App.Database
{
    public sealed record Response(Server Server, TimeSpan Runtime);

    public class UpdateRecordService(string connectionString)
    {
        public async Task<Response> Update(string databaseName, List<RawVillage> rawVillages)
        {
            var sw = Stopwatch.StartNew();

            await using var connection = await CreateDatabaseIfNotExsits(databaseName);

            var villages = DataManipulation.Villages(rawVillages);
            var players = DataManipulation.Players(rawVillages);
            var alliances = DataManipulation.Alliances(rawVillages);

            using var transaction = await connection.BeginTransactionAsync();

            // Unique runtime identifier for the staging session tables
            string sessionSuffix = Guid.NewGuid().ToString("N")[..8];
            string stgAlliances = $"`stg_alliances_{sessionSuffix}`";
            string stgPlayers = $"`stg_players_{sessionSuffix}`";
            string stgVillages = $"`stg_villages_{sessionSuffix}`";

            try
            {
                // ====================================================================
                // STEP 1: CREATE MEMORY-BACKED TEMPORARY STAGING TABLES
                // ====================================================================
                using (var cmd = new MySqlCommand($@"
                CREATE TEMPORARY TABLE {stgAlliances} LIKE `Alliances`;
                CREATE TEMPORARY TABLE {stgPlayers} LIKE `Players`;
                CREATE TEMPORARY TABLE {stgVillages} LIKE `Villages`;", connection, transaction))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // ====================================================================
                // STEP 2: BUILD STRING STREAM DATA BUFFERS
                // ====================================================================
                var allianceCsv = new StringBuilder();
                foreach (var a in alliances)
                    allianceCsv.AppendLine($"{a.Id}\t{EscapeCsv(a.Name)}\t{a.PlayerCount}");

                var playerCsv = new StringBuilder();
                foreach (var p in players)
                    playerCsv.AppendLine($"{p.Id}\t{p.AllianceId}\t{EscapeCsv(p.Name)}\t{p.Population}\t{p.VillageCount}\t0"); // Always 0 initially in file dump

                var villageCsv = new StringBuilder();
                foreach (var v in villages)
                {
                    string prevPlayer = v.PreviousPlayerId.HasValue ? v.PreviousPlayerId.Value.ToString() : "\\N";
                    villageCsv.AppendLine($"{v.Id}\t{v.MapId}\t{v.PlayerId}\t{prevPlayer}\t{EscapeCsv(v.Name)}\t{v.X}\t{v.Y}\t{v.Tribe}\t{v.Population}\t{EscapeCsv(v.Region)}\t{ToString(v.IsCapital)}\t{ToString(v.IsCity)}\t{ToString(v.IsHarbor)}\t{v.VictoryPoints}\t0");
                }

                // ====================================================================
                // STEP 3: NATIVE STREAM BULK LOAD INTO STAGING TABLES
                // ====================================================================
                await StreamLocalInfileAsync(connection, transaction, stgAlliances, allianceCsv.ToString(), "(`Id`, `Name`, `PlayerCount`)");
                await StreamLocalInfileAsync(connection, transaction, stgPlayers, playerCsv.ToString(), "(`Id`, `AllianceId`, `Name`, `Population`, `VillageCount`, `IsDeleted`)");
                await StreamLocalInfileAsync(connection, transaction, stgVillages, villageCsv.ToString(), "(`Id`, `MapId`, `PlayerId`, `PreviousPlayerId`, `Name`, `X`, `Y`, `Tribe`, `Population`, `Region`, `IsCapital`, `IsCity`, `IsHarbor`, `VictoryPoints`, `IsDestroyed`)");

                // ====================================================================
                // STEP 4: UPSERT INTO MAIN TABLES & AUTO-FLAG MISSING RECORDS
                // ====================================================================
                string processQueries = $@"
                -- A. Upsert Alliances (No modifications to deletion rules)
                INSERT INTO `Alliances` (`Id`, `Name`, `PlayerCount`)
                SELECT `Id`, `Name`, `PlayerCount` FROM {stgAlliances}
                ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name`), `PlayerCount` = VALUES(`PlayerCount`);

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
                    `PreviousPlayerId` = IF(`PlayerId` <> VALUES(`PlayerId`), `PlayerId`, `PreviousPlayerId`),

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

                // ====================================================================
                // STEP 5: DAILY HISTORY SNAPSHOT ENGINE (INCLUDING SOFT DELETED BROKEN DATA)
                // ====================================================================
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
            string resourceName = "App.Database.Scripts.CreateTables.sql";

            using Stream stream = (assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Could not find the embedded resource: {resourceName}")) ?? throw new FileNotFoundException($"Could not find the embedded resource: {resourceName}");
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        private static async Task StreamLocalInfileAsync(MySqlConnection conn, MySqlTransaction tx, string table, string csvData, string columns)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"{table}.tsv");
            await File.WriteAllTextAsync(tempPath, csvData, Encoding.UTF8);

            try
            {
                // MariaDB local stream load assignment structure mapped syntax
                string loadQuery = $"LOAD DATA LOCAL INFILE '{tempPath.Replace("\\", "/")}' " +
                                   $"INTO TABLE {table} " +
                                   $"FIELDS TERMINATED BY '\t' LINES TERMINATED BY '\n' " +
                                   $"{columns};";

                using var cmd = new MySqlCommand(loadQuery, conn, tx);
                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\t", " ").Replace("\n", " ").Replace("\r", " ");
        }

        private static string ToString(bool value) => value ? "1" : "0";
    }
}