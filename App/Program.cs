using App;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Settings.Configuration;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseConsoleLifetime()
        .ConfigureServices((hostBuilderContext, services) =>
        {
            services.AddSerilog(c => c
                .ReadFrom.Configuration(hostBuilderContext.Configuration,
        new ConfigurationReaderOptions(ConfigurationAssemblySource.AlwaysScanDllFiles)));
            services.AddHostedService<MainService>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Environment.ExitCode = 1;
    Log.Fatal(ex, "An unhandled exception occured during bootstrapping");
}
finally
{
    Log.CloseAndFlush();
}