using ConsoleTables;
using Immediate.Handlers.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ServerScanner.Models;

namespace ServerScanner.Commands
{
    [Handler]
    public static partial class ReadYourGameWorldCommand
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

            await LoadAllRegion(page, logger);
            var gameWorldDivs = await page.QuerySelectorAllAsync("div.gameworld");

            for (int i = 0; i < gameWorldDivs.Count; i++)
            {
                if (i != 0)
                {
                    await LoadAllRegion(page, logger);
                    gameWorldDivs = await page.QuerySelectorAllAsync("div.gameworld");
                }
                var gameWorldDiv = gameWorldDivs[i];
                (string? worldId, string worldName) = await GetWorldInfo(gameWorldDiv);
                logger.LogInformation("Found game world: ID = {WorldId}, Name = {WorldName}", worldId, worldName);

                if (string.IsNullOrEmpty(worldId) || serversInDb.Contains(worldId, StringComparer.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Skipping game world with ID = {WorldId} as it is already in the database.", worldId);
                    continue;
                }

                await gameWorldDiv.ClickAsync();
                await page.WaitForSelectorAsync("#gameworldDetails > section.action > form > button", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

                var dialogButton = await page.QuerySelectorAsync("#gameworldDetails > section.action > form > button");
                if (dialogButton != null)
                {
                    var oldUrl = page.Url;
                    await dialogButton.ClickAsync();
                    try
                    {
                        await page.WaitForURLAsync(url => !string.Equals(url, oldUrl, StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        logger.LogWarning("URL did not change after clicking the dialog button for game world: ID = {WorldId}, Name = {WorldName}. Ignored", worldId, worldName);
                        continue;
                    }

                    var currentUrl = page.Url;
                    logger.LogInformation("Navigated to URL: {Url}", currentUrl);
                    await page.GoBackAsync();
                    var server = new Server(worldId, worldName, currentUrl);
                    servers.Add(server);
                }
                else
                {
                    logger.LogWarning("Dialog button not found for game world: ID = {WorldId}, Name = {WorldName}", worldId, worldName);
                }
            }

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