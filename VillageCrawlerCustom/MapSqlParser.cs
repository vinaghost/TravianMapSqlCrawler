using System.Text;

namespace VillageCrawlerCustom
{
    public static class MapSqlParser
    {
        public static IList<RawVillage> Parse(StreamReader streamReader)
        {
            var villages = new List<RawVillage>();
            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                if (line is null) continue;
                var village = GetVillage(line);
                if (village is null) continue;
                villages.Add(village);
            }
            return villages;
        }

        private static RawVillage? GetVillage(string line)
        {
            if (string.IsNullOrEmpty(line)) return null;
            var villageLine = line.Remove(0, 30);
            villageLine = villageLine.Remove(villageLine.Length - 2, 2);
            var fields = villageLine.ParseLine();
            if (fields.Length != 16) return null;
            var mapId = int.Parse(fields[0]);
            var x = int.Parse(fields[1]);
            var y = int.Parse(fields[2]);
            var tribe = int.Parse(fields[3]);
            var villageId = int.Parse(fields[4]);
            var villageName = fields[5];
            var playerId = int.Parse(fields[6]);
            var playerName = fields[7];
            var allianceId = int.Parse(fields[8]);
            var allianceName = fields[9];
            var population = int.Parse(fields[10]);
            var region = fields[11];
            var isCapital = fields[12].Equals("TRUE");
            var isCity = fields[13].Equals("TRUE");
            var isHarbor = fields[14].Equals("TRUE");
            var victoryPoints = fields[15].Equals("NULL") ? 0 : int.Parse(fields[15]);
            return new RawVillage(mapId, x, y, tribe, villageId, villageName, playerId, playerName, allianceId, allianceName, population, region, isCapital, isCity, isHarbor, victoryPoints);
        }

        private static string Peek(this string source, int peek) => peek < 0 ? "" : source[..source.Claim(peek)];

        private static int Claim(this string source, int position) => source.Length < position ? source.Length : position;

        private static (string, string) Pop(this string source, int pop) => pop < 0 ? ("", source) : (source[..source.Claim(pop)], source.PopString(pop));

        private static string PopString(this string source, int pop) => source.Length < pop ? "" : source[pop..];

        public static string[] ParseLine(this string line)
        {
            return ParseLineImpl(line).ToArray();

            static IEnumerable<string> ParseLineImpl(string l)
            {
                string remainder = l;
                string field;
                while (remainder.Peek(1) != "")
                {
                    (field, remainder) = ParseField(remainder);
                    yield return field;
                }
            }
        }

        private const string GroupOpen = "'";
        private const string GroupClose = "'";

        private static (string field, string remainder) ParseField(string line)
        {
            if (line.Peek(1) == GroupOpen)
            {
                var (_, split) = line.Pop(1);
                return ParseFieldQuoted(split);
            }
            else
            {
                var (head, tail) = line.Pop(1);
                var sb = new StringBuilder();
                while (head != "," && head != "")
                {
                    sb.Append(head);
                    (head, tail) = tail.Pop(1);
                }
                return (sb.ToString(), tail);
            }
        }

        private static (string field, string remainder) ParseFieldQuoted(string line) => ParseFieldQuoted(line, false);

        private static (string field, string remainder) ParseFieldQuoted(string line, bool isNested)
        {
            var tail = line;

            var sb = new StringBuilder();

            while (tail.Peek(1) != "" && tail.Peek(1) != GroupClose)
            {
                if (tail.Peek(1) == GroupOpen)
                {
                    (_, tail) = tail.Pop(1);
                    (var head, tail) = ParseFieldQuoted(tail, true);
                    sb.Append(GroupOpen + head + GroupClose);
                }
                else
                {
                    (var head, tail) = tail.Pop(1);
                    sb.Append(head);
                }
            }
            if (tail.Peek(2) == GroupClose + ",")
            {
                (_, tail) = tail.Pop(isNested ? 1 : 2);
            }
            else if (tail.Peek(1) == GroupClose)
            {
                (_, tail) = tail.Pop(1);
            }
            return (sb.ToString(), tail);
        }
    }
}