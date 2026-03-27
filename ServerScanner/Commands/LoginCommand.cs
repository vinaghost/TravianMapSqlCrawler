using Immediate.Handlers.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.Extensions.Options;
using ServerScanner.Configuration;

namespace ServerScanner.Commands
{
    [Handler]
    public static partial class LoginCommand
    {
        public sealed record Command(IPage Page);

        private static async ValueTask HandleAsync(
            Command command,
            ILogger<Handler> logger,
            IOptions<LoginCredentials> credentialsOptions,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var credentials = credentialsOptions.Value;
            var page = command.Page;

            await page.GotoAsync("https://lobby.legends.travian.com/");

            await page.FillAsync("input[name='name']", credentials.Username);
            logger.LogInformation("Filled username");

            await page.FillAsync("input[name='password']", credentials.Password);
            logger.LogInformation("Filled password");

            await page.ClickAsync("button[type='submit']");
            logger.LogInformation("Clicked login button");
        }
    }
}