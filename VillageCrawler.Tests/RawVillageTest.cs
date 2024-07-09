using FluentAssertions;
using VillageCrawler.Entities;
using VillageCrawler.Extensions;
using VillageCrawler.Models;
using VillageCrawler.Parsers;

namespace VillageCrawler.Tests
{
    public class RawVillageTest
    {
        [Theory]
        [InlineData("data/01.sql", "data/02.sql", true)]
        [InlineData("data/02.sql", "data/02.sql", false)]
        public void GetAlliances_ChangePlayerCount_CorrectDifferent(string first, string second, bool expected)
        {
            // Arrange
            var firstVillages = GetRawVillages(first);
            var secondVillages = GetRawVillages(second);

            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            var firstAlliances = firstVillages.GetAlliances();
            var secondAlliances = secondVillages.GetAlliances().Values
                .Select(x => new
                {
                    x.Id,
                    x.PlayerCount,
                })
                .AsEnumerable()
                .Select(x => new AllianceHistory
                {
                    AllianceId = x.Id,
                    Date = yesterday,
                    PlayerCount = x.PlayerCount,
                })
                .ToList();
            // Act

            foreach (var alliance in secondAlliances)
            {
                var exist = firstAlliances.TryGetValue(alliance.AllianceId, out var todayAlliance);
                if (!exist) { continue; }
                if (todayAlliance is null) { continue; }
                alliance.ChangePlayerCount = todayAlliance.PlayerCount == alliance.PlayerCount;
            }

            var result = secondAlliances
                .Exists(x => !x.ChangePlayerCount);

            // Assert
            result
                .Should()
                .Be(expected);
        }

        private IList<RawVillage> GetRawVillages(string path)
        {
            using var file = File.OpenRead(path);
            using var reader = new StreamReader(file);
            return MapSqlParser.Parse(reader);
        }
    }
}