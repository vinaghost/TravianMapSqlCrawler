using App.Commands;
using Shouldly;

namespace TestProject.Commands
{
    public class GetServerDataCommandTest
    {
        [Fact]
        public async Task ReturnsListOfRawVillage()
        {
            // Arrange
            var content = File.OpenRead("TestData/map.sql");
            var streamReader = new StreamReader(content);

            var handleBehavior = new GetServerDataCommand.HandleBehavior();

            // Act
            var result = await handleBehavior.HandleAsync(new(streamReader), CancellationToken.None);

            // Assert
            result.ShouldNotBeEmpty();
            result.Count.ShouldBe(1827);
        }
    }
}