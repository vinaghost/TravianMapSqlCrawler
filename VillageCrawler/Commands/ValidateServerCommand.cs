using MediatR;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using VillageCrawler.DbContexts;
using VillageCrawler.Entities;
using VillageCrawler.Models.Options;

namespace VillageCrawler.Commands
{
    public record ValidateServerCommand : IRequest<IList<Server>>;

    public class ValidateServerCommandHandler(IHttpClientFactory httpClientFactory,
                                            IOptions<ConnectionStrings> connectionStrings,
                                            IOptions<AppSettings> appSettings)
        : IRequestHandler<ValidateServerCommand, IList<Server>>
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ConnectionStrings _connectionStrings = connectionStrings.Value;
        private readonly AppSettings _appSettings = appSettings.Value;

        public async Task<IList<Server>> Handle(ValidateServerCommand request, CancellationToken cancellationToken)
        {
            using var context = new ServerDbContext(_connectionStrings.Server);
            await context.Database.EnsureCreatedAsync(cancellationToken);

            var servers = context.Servers
                .ToList();

            var validServers = new ConcurrentQueue<Server>();

            await Parallel.ForEachAsync(servers, async (server, token) =>
            {
                var isValid = await ValidateServer(server.Url, token);
                if (!isValid) return;
                validServers.Enqueue(server);
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