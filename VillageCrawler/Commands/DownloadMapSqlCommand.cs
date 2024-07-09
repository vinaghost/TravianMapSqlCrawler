using MediatR;
using Microsoft.Extensions.Options;
using VillageCrawler.Models;
using VillageCrawler.Models.Options;
using VillageCrawler.Parsers;

namespace VillageCrawler.Commands
{
    public record DownloadMapSqlCommand(string Url) : IRequest<IList<RawVillage>>;

    public sealed class DownloadMapSqlCommandHandler(IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        : IRequestHandler<DownloadMapSqlCommand, IList<RawVillage>>
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly AppSettings _appSettings = appSettings.Value;

        public async Task<IList<RawVillage>> Handle(DownloadMapSqlCommand command, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var responseStream = await httpClient.GetStreamAsync(string.Format(_appSettings.UrlMapSql, command.Url), cancellationToken);
            using var reader = new StreamReader(responseStream);
            var villages = MapSqlParser.Parse(reader);
            return villages;
        }
    }
}