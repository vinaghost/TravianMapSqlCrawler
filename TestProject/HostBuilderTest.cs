using App;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace TestProject
{
    public class HostBuilderTest
    {
        [Fact]
        public void HostBuilder_ConfigureServices()
        {
            // Arrange
            var hostBuilder = new HostBuilder()
                .ConfigureServices()
                .UseDefaultServiceProvider((hostContext, config) =>
                {
                    config.ValidateOnBuild = true;
                    config.ValidateScopes = true;
                });
            // Act
            var host = hostBuilder.Build();
            // Assert
            host.ShouldNotBeNull();
        }
    }
}