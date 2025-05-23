using Immediate.Handlers.Shared;

namespace App.Commands
{
    [Handler]
    public static partial class GetMapSqlCommand
    {
        public sealed record Command(string Url);

        public const string UrlMapSqlTemplate = "https://{0}/map.sql";

        private static async ValueTask<StreamReader> HandleAsync(
            Command command,
            HttpClient httpClient,
            CancellationToken cancellationToken)
        {
            var url = string.Format(UrlMapSqlTemplate, command.Url);
            var responseStream = await httpClient.GetStreamAsync(url, cancellationToken);
            var streamReader = new StreamReader(responseStream);
            return streamReader;
        }
    }
}