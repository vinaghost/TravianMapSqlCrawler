using ConsoleTables;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using ServerScanner.Configuration;
using ServerScanner.Entities;
using ServerScanner.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerScanner.Commands
{
    [Handler]
    public static partial class ReadMyGameWorldCommand
    {
        public sealed record Command(IPage Page, IList<string> ServersInDb);

        private static async ValueTask<List<Server>> HandleAsync(
            Command command,
            ILogger<Handler> logger,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (page, serversInDb) = command;
            var servers = new List<Server>();

            var gameWorldDivs = await page.QuerySelectorAllAsync("div.gameworld");

            for (int i = 0; i < gameWorldDivs.Count; i++)
            {
                if (i != 0)
                {
                    gameWorldDivs = await page.QuerySelectorAllAsync("div.gameworld");
                }

                var gameWorldDiv = gameWorldDivs[i];
                (string? worldId, string worldName) = await GetWorldInfo(gameWorldDiv);
                logger.LogInformation("Found game world: ID = {WorldId}, Name = {WorldName}", worldId, worldName);

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
            return [.. servers.Where(x => !string.IsNullOrEmpty(x.Name))];
        }

        private static async Task<(string? worldId, string worldName)> GetWorldInfo(IElementHandle gameWorldDiv)
        {
            var worldId = await gameWorldDiv.GetAttributeAsync("data-wuid");

            var gameWorldNameDiv = await gameWorldDiv.QuerySelectorAsync("div.gameworldName");
            var worldName = gameWorldNameDiv != null ? await gameWorldNameDiv.InnerTextAsync() : "";
            return (worldId, worldName);
        }
    }
}