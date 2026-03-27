using ConsoleTables;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ServerScanner.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerScanner.Commands
{
    [Handler]
    public static partial class ReadMyGameWorldCommand
    {
        public sealed record Command(IPage Page);

        private static async ValueTask HandleAsync(
            Command command,
            ILogger<Handler> logger,
            CancellationToken cancellationToken)
        {
            var page = command.Page;

            // List to store Server instances
            var servers = new List<Server>();

            // Find all divs with class "gameworld"
            var gameWorldDivs = await page.QuerySelectorAllAsync("div.gameworld");

            for (int i = 0; i < gameWorldDivs.Count; i++)
            {
                // Re-fetch the gameWorldDivs to ensure the DOM is up-to-date
                gameWorldDivs = await page.QuerySelectorAllAsync("div.gameworld");
                var gameWorldDiv = gameWorldDivs[i];

                // Extract the "data-wuid" attribute for the world ID
                var worldId = await gameWorldDiv.GetAttributeAsync("data-wuid");

                // Find the child div with class "gameworldName" and extract its text for the world name
                var gameWorldNameDiv = await gameWorldDiv.QuerySelectorAsync("div.gameworldName");
                var worldName = gameWorldNameDiv != null ? await gameWorldNameDiv.InnerTextAsync() : null;

                // Log the extracted information
                logger.LogInformation("Found game world: ID = {WorldId}, Name = {WorldName}", worldId, worldName);

                // Find the button with class "playNow" and click it
                var playNowButton = await gameWorldDiv.QuerySelectorAsync("button.playNow");
                if (playNowButton != null)
                {
                    var oldUrl = page.Url;
                    await playNowButton.ClickAsync();
                    // Wait for the URL to change
                    await page.WaitForURLAsync(url => !string.Equals(url, oldUrl, StringComparison.OrdinalIgnoreCase));

                    var currentUrl = page.Url;
                    logger.LogInformation("Navigated to URL: {Url}", currentUrl);

                    // Create a Server instance and add it to the list
                    var server = new Server(worldId ?? "", worldName ?? "", currentUrl);
                    servers.Add(server);

                    // Navigate back to the server menu
                    await page.GoBackAsync();
                }
                else
                {
                    logger.LogWarning("PlayNow button not found for game world: ID = {WorldId}, Name = {WorldName}", worldId, worldName);
                }
            }

            // Log the collected servers
            logger.LogInformation("Collected {Count} servers.", servers.Count);
            ConsoleTable.From(servers).Write(Format.Alternative);
        }
    }
}