using Discord.Webhook;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServerCrawler.DbContexts;
using ServerCrawler.Entities;
using ServerCrawler.Models.Options;
using System.Text;

namespace ServerCrawler.Commands
{
    public record SendMessageDiscordCommand : IRequest;

    public class SendMessageDiscordCommandHandler(IOptions<AppSettings> appSettings,
                                                IOptions<ConnectionStrings> connectionStrings)
        : IRequestHandler<SendMessageDiscordCommand>
    {
        private readonly AppSettings _appSettings = appSettings.Value;
        private readonly ConnectionStrings _connections = connectionStrings.Value;

        public async Task Handle(SendMessageDiscordCommand request, CancellationToken cancellationToken)
        {
            var message = await GetMessage();
            if (string.IsNullOrEmpty(message)) return;
            using var client = new DiscordWebhookClient(_appSettings.WebhookUrl);
            await client.SendMessageAsync(message);
        }

        private async Task<string> GetMessage()
        {
            var todayServers = await GetServers(DateTime.Today);
            var threeDaysServers = await GetServers(DateTime.Today.AddDays(-3));

            if (todayServers.Count == 0 && threeDaysServers.Count == 0)
            {
                return "";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"@everyone {_appSettings.TravianLobbyUrl}");

            if (todayServers.Count > 0)
            {
                sb.Append("Today's servers: ");
                foreach (var server in todayServers)
                {
                    sb.Append($"{server.Name}, ");
                }

                sb.AppendLine();
            }

            if (threeDaysServers.Count > 0)
            {
                sb.Append("Three days ago servers: ");
                foreach (var server in threeDaysServers)
                {
                    sb.Append($"{server.Name}, ");
                }
            }

            return sb.ToString();
        }

        private async Task<List<Server>> GetServers(DateTime dateTime)
        {
            using var context = new CalendarDbContext(_connections.Calendar);

            var servers = await context.Servers
                .Where(x => x.StartDate == dateTime)
                .ToListAsync();

            return servers;
        }
    }
}