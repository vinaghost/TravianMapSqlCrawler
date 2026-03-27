using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ServerScanner.Configuration;

namespace ServerScanner
{
    public static class AppMixins
    {
        public static IHostBuilder ConfigureCoreServices(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((hostBuilderContext, services) =>
            {
                services.Configure<LoginCredentials>(hostBuilderContext.Configuration.GetSection(nameof(LoginCredentials)));
                services.AddSerilog(c => c
                    .ReadFrom.Configuration(hostBuilderContext.Configuration));
            });

        public static IHostBuilder ConfigureServices(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((services) =>
            {
                services.AddHostedService<MainService>();
                services.AddServerScannerHandlers();
                services.AddServerScannerBehaviors();
            });
    }
}