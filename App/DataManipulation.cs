using App.Entities;
using App.Models;

namespace App
{
    public static class DataManipulation
    {
        public static List<Alliance> Alliances(List<RawVillage> rawVillages)
        {
            var alliances = rawVillages
                .DistinctBy(x => x.PlayerId)
                .GroupBy(x => x.AllianceId)
                .Select(x => new Alliance
                {
                    Id = x.Key,
                    Name = x.Select(x => x.AllianceName).First(),
                    PlayerCount = x.Count(),
                })
                .ToList();
            return alliances;
        }

        public static List<Player> Players(List<RawVillage> rawVillages)
        {
            var players = rawVillages
                .GroupBy(x => x.PlayerId)
                .Select(x => new Player
                {
                    Id = x.Key,
                    Name = x.Select(x => x.PlayerName).First(),
                    AllianceId = x.Select(x => x.AllianceId).First(),
                    Population = x.Sum(x => x.Population),
                    VillageCount = x.Count(),
                })
                .ToList();
            return players;
        }

        public static List<Village> Villages(List<RawVillage> rawVillages)
        {
            var villages = rawVillages
                .Select(x => new Village
                {
                    Id = x.VillageId,
                    MapId = x.MapId,
                    Name = x.VillageName,
                    Tribe = x.Tribe,
                    X = x.X,
                    Y = x.Y,
                    PlayerId = x.PlayerId,
                    IsCapital = x.IsCapital,
                    IsCity = x.IsCity,
                    IsHarbor = x.IsHarbor,
                    Population = x.Population,
                    Region = x.Region,
                    VictoryPoints = x.VictoryPoints
                })
                .ToList();
            return villages;
        }
    }
}