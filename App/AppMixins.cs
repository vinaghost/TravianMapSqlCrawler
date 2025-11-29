using App.Commands;
using App.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace App
{
    public static class AppMixins
    {
        public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((hostBuilderContext, services) =>
            {
                services.AddSerilog(c => c
                    .ReadFrom.Configuration(hostBuilderContext.Configuration));
            });

        public static IHostBuilder BindConfiguration(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((hostBuilderContext, services) =>
            {
                services.Configure<ConnectionStrings>(hostBuilderContext.Configuration.GetSection(nameof(ConnectionStrings)));
            });

        public static IHostBuilder ConfigureServices(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((services) =>
            {
                services.AddHostedService<MainService>();
                services.AddScoped<DataService>();
                services.AddHttpClient<GetMapSqlCommand.Handler>();
                services.AddHttpClient<GetServerListCommand.Handler>();
                services.AddAppHandlers();
                services.AddAppBehaviors();
            });
    }
}