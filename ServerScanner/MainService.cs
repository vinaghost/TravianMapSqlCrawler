using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ServerScanner.Commands;

namespace ServerScanner
{
    public class MainService(IHostApplicationLifetime hostApplicationLifetime,
                             IServiceScopeFactory serviceScopeFactory,
                             ILogger<MainService> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();
            logger.LogInformation("Starting server scanner...");
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Firefox.LaunchAsync();
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var loginCommand = scope.ServiceProvider.GetRequiredService<LoginCommand.Handler>();
            await loginCommand.HandleAsync(new(page), cancellationToken);
            await Task.Delay(5000);

            var readMyGameWorldCommand = scope.ServiceProvider.GetRequiredService<ReadMyGameWorldCommand.Handler>();
            await readMyGameWorldCommand.HandleAsync(new(page), cancellationToken);

            var expandGameWorldCommand = scope.ServiceProvider.GetRequiredService<ExpandGameWorldCommand.Handler>();
            await expandGameWorldCommand.HandleAsync(new(page), cancellationToken);
            await Task.Delay(5000);

            await page.ScreenshotAsync(new() { Path = "screenshot.png", FullPage = true });

            var readYourGameWorldCommand = scope.ServiceProvider.GetRequiredService<ReadYourGameWorldCommand.Handler>();
            await readYourGameWorldCommand.HandleAsync(new(page), cancellationToken);

            await Task.CompletedTask;
            hostApplicationLifetime.StopApplication();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}