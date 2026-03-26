using Immediate.Handlers.Shared;
using System.Diagnostics;

namespace App.Commands
{
    [Handler]
    public static partial class GetMapSqlCommand
    {
        public sealed record Command(string Url);

        public sealed record Response(StreamReader MapSqlStream, TimeSpan Runtime);

        public const string UrlMapSqlTemplate = "https://{0}/map.sql";

        private static async ValueTask<Response> HandleAsync(
            Command command,
            HttpClient httpClient,
            CancellationToken cancellationToken)
        {
            var url = string.Format(UrlMapSqlTemplate, command.Url);

            var sw = Stopwatch.StartNew();
            var response = await httpClient.GetAsync(url, cancellationToken);
            var streamReader = new StreamReader(response.Content.ReadAsStream(cancellationToken));
            sw.Stop();
            return new(streamReader, sw.Elapsed);
        }
    }
}