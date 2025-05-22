using App.Commands;
using NSubstitute;
using RichardSzalay.MockHttp;
using Shouldly;

namespace TestProject.Commands
{
    public class GetServerListCommandTest
    {
        [Fact]
        public async Task Return_CorrectData()
        {
            // Arrange
            var content = File.ReadAllText("HtmlFiles/TravMapServerList.html");
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, GetServerListCommand.Url)
                .Respond("text/html", content);

            var httpClient = mockHttp.ToHttpClient();
            var handleBehavior = new GetServerListCommand.HandleBehavior(httpClient);

            // Act
            var result = await handleBehavior.HandleAsync(new(), CancellationToken.None);

            // Assert
            result.Count.ShouldBe(56);
            result.ShouldAllBe(server => server.Length > 0);
        }
    }
}