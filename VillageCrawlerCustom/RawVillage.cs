using VillageCrawlerCustom.Entities;

namespace VillageCrawlerCustom
{
    public class RawVillage(int mapId, int x, int y, int tribe, int villageId, string villageName, int playerId, string playerName, int allianceId, string allianceName, int population, string region, bool isCapital, bool isCity, bool isHarbor, int victoryPoints)
    {
        public int MapId { get; set; } = mapId;
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
        public int Tribe { get; set; } = tribe;
        public int VillageId { get; set; } = villageId;
        public string VillageName { get; set; } = villageName;
        public int PlayerId { get; set; } = playerId;
        public string PlayerName { get; set; } = playerName;
        public int AllianceId { get; set; } = allianceId;
        public string AllianceName { get; set; } = allianceName;
        public int Population { get; set; } = population;
        public string Region { get; set; } = region;
        public bool IsCapital { get; set; } = isCapital;
        public bool IsCity { get; set; } = isCity;
        public bool IsHarbor { get; set; } = isHarbor;
        public int VictoryPoints { get; set; } = victoryPoints;
    }

    public static class RawVillageExtension
    {
        private static Village GetVillage(this RawVillage rawVillage)
        {
            return new Village
            {
                Id = rawVillage.VillageId,
                MapId = rawVillage.MapId,
                Name = rawVillage.VillageName,
                Tribe = rawVillage.Tribe,
                X = rawVillage.X,
                Y = rawVillage.Y,
                PlayerId = rawVillage.PlayerId,
                IsCapital = rawVillage.IsCapital,
                IsCity = rawVillage.IsCity,
                IsHarbor = rawVillage.IsHarbor,
                Population = rawVillage.Population,
                Region = rawVillage.Region,
                VictoryPoints = rawVillage.VictoryPoints
            };
        }

        public static List<Alliance> GetAlliances(this IList<RawVillage> rawVillages)
        {
            return rawVillages
                .DistinctBy(x => x.PlayerId)
                .GroupBy(x => x.AllianceId)
                .Select(x => new Alliance
                {
                    Id = x.Key,
                    Name = x.First().AllianceName,
                    PlayerCount = x.Count(),
                })
                .ToList();
        }

        public static List<Player> GetPlayers(this IList<RawVillage> rawVillages)
        {
            return rawVillages
               .GroupBy(x => x.PlayerId)
               .Select(x => new Player
               {
                   Id = x.Key,
                   Name = x.First().PlayerName,
                   AllianceId = x.First().AllianceId,
                   Population = x.Sum(x => x.Population),
                   VillageCount = x.Count(),
               })
               .ToList();
        }

        public static List<Village> GetVillages(this IList<RawVillage> rawVillages)
        {
            return rawVillages
                .Select(x => x.GetVillage())
                .ToList();
        }
    }
}