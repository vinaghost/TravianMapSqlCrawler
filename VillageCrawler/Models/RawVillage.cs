namespace VillageCrawler.Models
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
}