using HtmlAgilityPack;
using MediatR;
using Microsoft.Extensions.Options;
using ServerCrawler.Models;
using ServerCrawler.Models.Options;
using System.Text.RegularExpressions;

namespace ServerCrawler.Commands
{
    public record DownloadCalendarCommand : IRequest<IList<RawServer>>;

    public partial class DownloadCalendarCommandHandler(IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        : IRequestHandler<DownloadCalendarCommand, IList<RawServer>>
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly AppSettings _appSettings = appSettings.Value;

        public async Task<IList<RawServer>> Handle(DownloadCalendarCommand request, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var responseStream = await httpClient.GetStreamAsync(_appSettings.CalendarUrl, cancellationToken);
            using var reader = new StreamReader(responseStream);

            var html = new HtmlDocument();
            html.Load(reader);

            var calendars = html.DocumentNode
                .Descendants("a")
                .Where(x => x.HasClass("tribe-events-calendar-list__event-title-link"))
                .Select(x => x.InnerText)
                .Select(x => TabNewLineFinder().Replace(x, ""))
                .Select(GetServer);
            return calendars.ToList();
        }

        [GeneratedRegex(@"\t|\n|\r")]
        private static partial Regex TabNewLineFinder();

        private RawServer GetServer(string serverInfo)
        {
            var parts = serverInfo.Split('~', StringSplitOptions.RemoveEmptyEntries);
            var date = DateTime.ParseExact(parts[^1].Trim(), "dd.MM.yyyy", new TravianDateFormat());

            if (parts.Length == 3)
            {
                return new RawServer(parts[0].Trim() + " " + parts[1].Trim(), date);
            }
            else
            {
                return new RawServer(parts[0].Trim(), date);
            }
        }
    }

    public class TravianDateFormat : IFormatProvider, ICustomFormatter
    {
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (arg is DateTime date)
            {
                return date.ToString("dd.MM.yyyy");
            }

            return arg?.ToString() ?? "";
        }

        public object? GetFormat(Type? formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }
    }
}