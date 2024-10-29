using System.Text;

namespace VillageCrawlerCustom.Extensions
{
    public static class MapSqlParserExtensions
    {
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