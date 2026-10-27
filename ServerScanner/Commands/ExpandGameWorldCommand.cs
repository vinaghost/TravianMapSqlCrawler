using Immediate.Handlers.Shared;

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ServerScanner.Commands
{
    [Handler]
    public static partial class ExpandGameWorldCommand
    {
        public sealed record Command(IPage Page);

        private static async ValueTask HandleAsync(
            Command command,
            ILogger<Handler> logger,
            CancellationToken cancellationToken)
        {
            var page = command.Page;

            await page.ClickAsync("div.headerContainerStart button.framed.green.withText");
            logger.LogInformation("Clicked Join new game world.");
        }
    }
}