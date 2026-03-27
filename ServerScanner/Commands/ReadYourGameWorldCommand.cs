using ConsoleTables;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Serilog.Core;
using ServerScanner.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerScanner.Commands
{
    [Handler]
    public static partial class ReadYourGameWorldCommand
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

            await LoadAllRegion(page, logger);
            // Find all divs with class "gameworld"
            var gameWorldDivs = await page.QuerySelectorAllAsync("div.gameworld");

            for (int i = 0; i < gameWorldDivs.Count; i++)
            {
                // Re-fetch the gameWorldDivs to ensure the DOM is up-to-date
                await LoadAllRegion(page, logger);
                gameWorldDivs = await page.QuerySelectorAllAsync("div.gameworld");
                var gameWorldDiv = gameWorldDivs[i];

                // Extract the "data-wuid" attribute for the world ID
                var worldId = await gameWorldDiv.GetAttributeAsync("data-wuid");

                // Find the child div with class "gameworldName" and extract its text for the world name
                var gameWorldNameDiv = await gameWorldDiv.QuerySelectorAsync("div.gameworldName");
                var worldName = gameWorldNameDiv != null ? await gameWorldNameDiv.InnerTextAsync() : null;

                // Log the extracted information
                logger.LogInformation("Found game world: ID = {WorldId}, Name = {WorldName}", worldId, worldName);

                // Click on the gameWorldDiv to open the dialog
                await gameWorldDiv.ClickAsync();

                // Wait for the dialog to appear
                await page.WaitForSelectorAsync("#gameworldDetails > section.action > form > button", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

                // Click the button inside the dialog
                var dialogButton = await page.QuerySelectorAsync("#gameworldDetails > section.action > form > button");
                if (dialogButton != null)
                {
                    var oldUrl = page.Url;
                    await dialogButton.ClickAsync();
                    try
                    {
                        // Wait for the URL to change
                        await page.WaitForURLAsync(url => !string.Equals(url, oldUrl, StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        logger.LogWarning("URL did not change after clicking the dialog button for game world: ID = {WorldId}, Name = {WorldName}. Ignored", worldId, worldName);
                        continue;
                    }

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
                    logger.LogWarning("Dialog button not found for game world: ID = {WorldId}, Name = {WorldName}", worldId, worldName);
                }
            }

            // Log the collected servers
            logger.LogInformation("Collected {Count} servers.", servers.Count);
            ConsoleTable.From(servers).Write(Format.Alternative);
        }

        private static async ValueTask LoadAllRegion(IPage page, ILogger logger)
        { // Wait for the buttons to be loaded on the page
            await page.WaitForSelectorAsync("button.regionFilterItem", new PageWaitForSelectorOptions
            {
                State = WaitForSelectorState.Visible,
            });

            var buttons = await page.QuerySelectorAllAsync("button.regionFilterItem");

            foreach (var button in buttons)
            {
                // Check if the button does not have the class "selected"
                var hasSelectedClass = await button.EvaluateAsync<bool>("el => el.classList.contains('selected')");

                if (!hasSelectedClass)
                {
                    // Retrieve the value of the "data-region" attribute
                    var dataRegion = await button.GetAttributeAsync("data-region");

                    // Click the button
                    await button.ClickAsync();

                    // Log the action with the data-region information
                    logger.LogInformation("Clicked '{DataRegion}' buton.", dataRegion);
                }
            }
        }
    }
}