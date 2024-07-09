using FluentAssertions;
using VillageCrawler.Parsers;

namespace VillageCrawler.Tests
{
    public class MapSqlParserTest
    {
        [Theory]
        [InlineData("data/01.sql", 4643)]
        [InlineData("data/02.sql", 4789)]
        public void Parse_File_CorrectLenght(string data, int expected)
        {
            // Arrange
            using var file = File.OpenRead(data);
            using var reader = new StreamReader(file);

            // Act
            var villages = MapSqlParser.Parse(reader);

            // Assert
            villages.Count
                .Should()
                .Be(expected);
        }
    }
}