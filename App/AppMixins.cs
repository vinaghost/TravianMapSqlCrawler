﻿using Microsoft.Extensions.DependencyInjection;
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

        public static IHostBuilder ConfigureServices(this IHostBuilder hostBuilder) =>
            hostBuilder.ConfigureServices((services) =>
            {
                services.AddHostedService<MainService>();

                services.AddHttpClient();
                services.AddAppHandlers();
                services.AddAppBehaviors();
            });
    }
}