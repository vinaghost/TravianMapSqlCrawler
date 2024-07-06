using HtmlAgilityPack;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerCrawler.Models;
using ServerCrawler.Models.Options;
using System.Text.RegularExpressions;

namespace ServerCrawler.Commands
{
    public record DownloadCalendarCommand : IRequest;

    public partial class DownloadCalendarCommandHandler(IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings, ILogger<DownloadCalendarCommand> logger)
        : IRequestHandler<DownloadCalendarCommand>
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly AppSettings _appSettings = appSettings.Value;
        private readonly ILogger<DownloadCalendarCommand> _logger = logger;

        public async Task Handle(DownloadCalendarCommand request, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var responseStream = await httpClient.GetStreamAsync(_appSettings.CalendarUrl, cancellationToken);
            using var reader = new StreamReader(responseStream);

            var html = new HtmlDocument();
            html.Load(reader);

            var calendars = html.DocumentNode
                .Descendants("a")
                .Where(x => x.HasClass("tribe-events-calendar-list__event-title-link"))
                .Select(x => TabNewLineFinder().Replace(x.InnerText, ""))
                .Select(x => GetServer(x));

            _logger.LogInformation("{Calendars}", calendars);
        }

        [GeneratedRegex(@"\t|\n|\r")]
        private static partial Regex TabNewLineFinder();

        private RawServer? GetServer(string serverInfo)
        {
            var parts = serverInfo.Split('~', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                _logger.LogWarning("Invalid server info: {ServerInfo}", serverInfo);
                return null;
            }

            var date = DateTime.Parse(parts[2].Trim(), new TravianDateFormat());
            var splits = parts[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

            var region = splits[0].Trim();
            var code = "";
            if (splits.Length == 2)
            {
                code = splits[1].Trim();
            }

            splits = parts[0].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            var speed = splits.Length == 2 ? splits[1].Trim() : "TBA";

            return new RawServer(region, speed, code, date);
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