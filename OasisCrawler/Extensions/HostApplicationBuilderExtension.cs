using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OasisCrawler.Models.Options;

namespace OasisCrawler.Extensions
{
    public static class HostApplicationBuilderExtension
    {
        public static IHostApplicationBuilder BindConfiguration(this IHostApplicationBuilder builder)
        {
            builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection(nameof(ConnectionStrings)));
            builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));

            return builder;
        }
    }
}