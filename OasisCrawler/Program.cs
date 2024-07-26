using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OasisCrawler;
using OasisCrawler.Extensions;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.BindConfiguration();
builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration));
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
builder.Services.AddHttpClient();
builder.Services.AddHostedService<StartUp>();

var host = builder.Build();
await host.RunAsync();