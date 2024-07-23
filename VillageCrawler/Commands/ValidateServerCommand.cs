using HtmlAgilityPack;
using MediatR;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using VillageCrawler.Entities;
using VillageCrawler.Models.Options;

namespace VillageCrawler.Commands
{
    public record ValidateServerCommand : IRequest<IList<Server>>;

    public class ValidateServerCommandHandler(IHttpClientFactory httpClientFactory,
                                            IOptions<AppSettings> appSettings)
        : IRequestHandler<ValidateServerCommand, IList<Server>>
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly AppSettings _appSettings = appSettings.Value;
        private const string _url = "https://travcotools.com/en/inactive-search/";

        public async Task<IList<Server>> Handle(ValidateServerCommand request, CancellationToken cancellationToken)
        {
            var servers = await GetServer(cancellationToken);
            var validServers = new ConcurrentQueue<Server>();

            await Parallel.ForEachAsync(servers, async (server, token) =>
            {
                var isValid = await ValidateServer(server.Url, token);
                if (!isValid) return;
                validServers.Enqueue(server);
            });

            return [.. validServers];
        }

        private async Task<List<Server>> GetServer(CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var html = await httpClient.GetStreamAsync(_url, cancellationToken);
            var doc = new HtmlDocument();
            doc.Load(html);

            var select = doc.DocumentNode.Descendants("select").FirstOrDefault(x => x.GetAttributeValue("name", "") == "travian_server");
            if (select is null) return [];

            var options = select.Descendants("option")
                .Where(x => !string.IsNullOrEmpty(x.GetAttributeValue("value", "")))
                .Select(x => new Server()
                {
                    Id = x.GetAttributeValue("value", 0),
                    Url = x.InnerText,
                });
            return options.ToList();
        }

        private async Task<bool> ValidateServer(string url, CancellationToken cancellationToken)
        {
            var urlMapSql = string.Format(_appSettings.UrlMapSql, url);
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, urlMapSql), cancellationToken);
                if (!response.IsSuccessStatusCode) return false;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}