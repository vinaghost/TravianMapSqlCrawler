using Immediate.Handlers.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using ServerScanner.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

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

            await page.ClickAsync("#root > header > div.headerContainerStart > button");
            logger.LogInformation("Clicked Join new game world.");
        }
    }
}