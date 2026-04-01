using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ServerScanner.Commands;

namespace ServerScanner
{
    public class MainService(IHostApplicationLifetime hostApplicationLifetime,
                             IServiceScopeFactory serviceScopeFactory) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Firefox.LaunchAsync();
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await page.SetViewportSizeAsync(1920, 1080);
            
            var loginCommand = scope.ServiceProvider.GetRequiredService<LoginCommand.Handler>();
            await loginCommand.HandleAsync(new(page), cancellationToken);

            var getServerCommand = scope.ServiceProvider.GetRequiredService<GetServerCommand.Handler>();
            var servers = await getServerCommand.HandleAsync(new(), cancellationToken);

            var readMyGameWorldCommand = scope.ServiceProvider.GetRequiredService<ReadMyGameWorldCommand.Handler>();
            var myServers = await readMyGameWorldCommand.HandleAsync(new(page, servers), cancellationToken);

            var expandGameWorldCommand = scope.ServiceProvider.GetRequiredService<ExpandGameWorldCommand.Handler>();
            await expandGameWorldCommand.HandleAsync(new(page), cancellationToken);

            var readYourGameWorldCommand = scope.ServiceProvider.GetRequiredService<ReadYourGameWorldCommand.Handler>();
            var yourServers = await readYourGameWorldCommand.HandleAsync(new(page, servers), cancellationToken);

            var updateServerCommand = scope.ServiceProvider.GetRequiredService<UpdateServerCommand.Handler>();
            await updateServerCommand.HandleAsync(new([.. yourServers, .. myServers]), cancellationToken);

            var updateShishnetCommand = scope.ServiceProvider.GetRequiredService<UpdateShishnetCommand.Handler>();
            await updateShishnetCommand.HandleAsync(new(), cancellationToken);

            hostApplicationLifetime.StopApplication();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
