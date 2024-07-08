using MediatR;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using VillageCrawler.DbContexts;
using VillageCrawler.Models.Options;

namespace VillageCrawler.Commands
{
    public record ValidateServerCommand : IRequest<IList<string>>;

    public class ValidateServerCommandHandler(IHttpClientFactory httpClientFactory,
                                            IOptions<ConnectionStrings> connectionStrings,
                                            IOptions<AppSettings> appSettings)
        : IRequestHandler<ValidateServerCommand, IList<string>>
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ConnectionStrings _connectionStrings = connectionStrings.Value;
        private readonly AppSettings _appSettings = appSettings.Value;

        public async Task<IList<string>> Handle(ValidateServerCommand request, CancellationToken cancellationToken)
        {
            using var context = new ServerDbContext(_connectionStrings.Server);
            await context.Database.EnsureCreatedAsync(cancellationToken);

            var servers = context.Servers
                .Select(x => x.Url)
                .ToList();

            var validServers = new ConcurrentQueue<string>();

            await Parallel.ForEachAsync(servers, async (serverUrl, token) =>
            {
                var isValid = await ValidateServer(serverUrl, token);
                if (!isValid) return;
                validServers.Enqueue(serverUrl);
            });

            return [.. validServers];
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