using App.Models;
using Immediate.Handlers.Shared;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace App.Commands
{
    [Handler]
    public static partial class GetRawVillageCommand
    {
        public sealed record Command(StreamReader StreamReader);
        public sealed record Response(List<RawVillage> RawVillages, TimeSpan Runtime);

        private static async ValueTask<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            var villages = new List<RawVillage>();
            var streamReader = command.StreamReader;

            var sw = Stopwatch.StartNew();
            string? line;
            while ((line = await streamReader.ReadLineAsync(cancellationToken)) is not null)
            {
                var village = GetVillage(line);
                if (village is not null)
                {
                    villages.Add(village);
                }
            }
            sw.Stop();
            return new(villages, sw.Elapsed);
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