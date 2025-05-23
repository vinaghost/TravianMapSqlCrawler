using App.Commands;
using RichardSzalay.MockHttp;
using Shouldly;

namespace TestProject.Commands
{
    public class GetMapSqlCommandTest
    {
        [Fact]
        public async Task Return_CorrectData()
        {
            // Arrange
            var url = "example.com";
            var content = File.ReadAllText("TestData/map.sql");
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, string.Format(GetMapSqlCommand.UrlMapSqlTemplate, url))
                .Respond("text/plain", content);
            var httpClient = mockHttp.ToHttpClient();
            var handleBehavior = new GetMapSqlCommand.HandleBehavior(httpClient);
            // Act
            using var streamReader = await handleBehavior.HandleAsync(new(url), CancellationToken.None);
            var result = await streamReader.ReadToEndAsync();
            // Assert
            result.ShouldNotBeNullOrEmpty();
            result.ShouldBe(content);
        }
    }
}