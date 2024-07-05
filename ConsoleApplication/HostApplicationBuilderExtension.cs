using ConsoleApplication.Models.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleApplication
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