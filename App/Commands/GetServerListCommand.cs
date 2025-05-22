using HtmlAgilityPack;
using Immediate.Handlers.Shared;

namespace App.Commands
{
    [Handler]
    public static partial class GetServerListCommand
    {
        public sealed record Command;

        public const string Url = "https://travmap.shishnet.org/status.php";

        private static async ValueTask<List<string>> HandleAsync(
            Command _,
            HttpClient httpClient,
            CancellationToken cancellationToken)
        {
            var response = await httpClient.GetAsync(Url);
            response.EnsureSuccessStatusCode();
            var html = new HtmlDocument();
            html.Load(response.Content.ReadAsStream());

            var table = html.DocumentNode
                .Descendants("table")
                .FirstOrDefault();
            if (table is null) return [];

            var rows = table
                .Descendants("tr")
                .Select(row => row.Descendants("td"))
                .Where(cells => cells.Count() > 3)
                .Select(cells => cells.ToList())
                .Where(cells => cells[2].InnerText.Trim() == "ok")
                .Select(cells => cells[0].InnerText.Trim())
                .ToList();
            return rows;
        }
    }
}