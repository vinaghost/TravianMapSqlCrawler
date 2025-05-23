using App;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseConsoleLifetime()
        .BindConfiguration()
        .ConfigureLogging()
        .ConfigureServices()
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occured during bootstrapping");
}
finally
{
    Log.CloseAndFlush();
}