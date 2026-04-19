using Immediate.Handlers.Shared;
using System.Diagnostics;

namespace App.Commands
{
    [Handler]
    public static partial class GetMapSqlCommand
    {
        public sealed record Command(string Url);

        public sealed record Response(StreamReader? MapSqlStream, TimeSpan Runtime);

        public const string UrlMapSqlTemplate = "https://{0}/map.sql";

        private static async ValueTask<Response> HandleAsync(
            Command command,
            HttpClient httpClient,
            CancellationToken cancellationToken)
        {
            var url = string.Format(UrlMapSqlTemplate, command.Url);

            var sw = Stopwatch.StartNew();

            var response = await GetResponse(httpClient, url, cancellationToken);
            if (response is null)
            {
                return new Response(null, sw.Elapsed);
            }

            var streamReader = new StreamReader(response.Content.ReadAsStream(cancellationToken));

            sw.Stop();
            return new(streamReader, sw.Elapsed);
        }

        private static async ValueTask<HttpResponseMessage?> GetResponse(HttpClient httpClient, string url, CancellationToken cancellationToken)
        {
            try
            {
                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                return response;
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
    }
}