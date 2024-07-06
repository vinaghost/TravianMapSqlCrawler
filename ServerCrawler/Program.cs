using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ServerCrawler;
using ServerCrawler.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.BindConfiguration();
builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
builder.Services.AddHttpClient();
builder.Services.AddHostedService<StartUp>();

var host = builder.Build();
await host.RunAsync();