using CSharpDiscordWebhook;
using CSharpDiscordWebhook.Objects;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ServerScanner.Configuration;
using ServerScanner.Entities;
using ServerScanner.Models;
using System.Drawing;

namespace ServerScanner.Commands
{
    [Handler]
    public static partial class UpdateServerCommand
    {
        public sealed record Command(IList<Server> Servers);

        private static async ValueTask HandleAsync(
            Command command,
            IOptions<ConnectionStrings> connectionStringsOptions,
            IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var servers = command.Servers;
            if (servers.Count == 0) return;
            var connectionStrings = connectionStringsOptions.Value.Server;
            await using var context = new ServerDbContext(connectionStrings);
            await context.AddRangeAsync(servers.Select(x => new LobbyServer()
            {
                Id = x.Id,
                Name = x.Name,
                UpdateAt = DateTime.UtcNow,
                Url = x.Url,
            }), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var urls = servers
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Url.Replace(".travian.com", "", StringComparison.OrdinalIgnoreCase))
                .ToList();

            using var webhook = new DiscordWebhook(new Uri(configuration["DiscordWebhookUrl"]!));
            await webhook.SendMessageAsync(new MessageBuilder
            {
                Embeds =
                [
                    new EmbedBuilder
                    {
                        Title = "Scan new servers",
                        Description = $"{servers.Count} servers added at <t:{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}:f>",
                        Fields = [
                            new EmbedFieldBuilder()
                            {
                                Name = "Server",
                                Value = string.Join("\n", urls),
                                Inline = true,
                            },
                        ],
                        Color = Color.Green,
                    }
                ],
            });
        }
    }
}