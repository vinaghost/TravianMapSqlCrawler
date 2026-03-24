using App.Models;
using Immediate.Handlers.Shared;
using System.Text;
using System.Text.RegularExpressions;

namespace App.Commands
{
    [Handler]
    public static partial class GetVillageDataCommand
    {
        public sealed record Command(StreamReader StreamReader);

        private static async ValueTask<List<RawVillage>> HandleAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            var villages = new List<RawVillage>();
            var streamReader = command.StreamReader;

            // Define batch sizes
            const int batchSize = 500; // Number of lines per batch
            const int maxConcurrentBatches = 10; // Number of batches to process concurrently

            var lines = new List<string>(batchSize);
            var batchTasks = new List<Task<List<RawVillage>>>(maxConcurrentBatches);

            string? line;
            while ((line = await streamReader.ReadLineAsync(cancellationToken)) is not null)
            {
                if (line is null) continue;
                lines.Add(line);
                if (lines.Count >= batchSize)
                {
                    // Create a copy of the lines list and process the batch
                    var batchCopy = new List<string>(lines);
                    batchTasks.Add(Task.Run(() => ProcessBatch(batchCopy), cancellationToken));
                    lines.Clear(); // Reset the lines list

                    // If the maximum number of concurrent batches is reached, await them
                    if (batchTasks.Count >= maxConcurrentBatches)
                    {
                        var completedBatches = await Task.WhenAll(batchTasks);
                        foreach (var batch in completedBatches)
                        {
                            villages.AddRange(batch);
                        }
                        batchTasks.Clear(); // Clear the completed tasks
                    }
                }
            }

            // Process any remaining lines
            if (lines.Count > 0)
            {
                batchTasks.Add(Task.Run(() => ProcessBatch(lines), cancellationToken));
            }

            // Await any remaining tasks
            if (batchTasks.Count > 0)
            {
                var completedBatches = await Task.WhenAll(batchTasks);
                foreach (var batch in completedBatches)
                {
                    villages.AddRange(batch);
                }
            }

            return villages;
        }

        private static List<RawVillage> ProcessBatch(List<string> lines)
        {
            var villages = new List<RawVillage>();
            foreach (var line in lines)
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
    }
}