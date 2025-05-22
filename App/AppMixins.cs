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
    }
}