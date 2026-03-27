using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using ServerScanner;
using Microsoft.Extensions.Options;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseConsoleLifetime()
        .ConfigureCoreServices()
        .ConfigureServices()
        .Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occured during bootstrapping");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}