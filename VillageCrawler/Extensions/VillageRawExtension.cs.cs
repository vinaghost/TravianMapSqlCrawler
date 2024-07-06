using VillageCrawler.Entities;
using VillageCrawler.Models;

namespace VillageCrawler.Extensions
{
    public static class RawVillageExtension
    {
        public static Alliance GetAlliace(this RawVillage rawVillage)
        {
            return new Alliance
            {
                Id = rawVillage.AllianceId,
                Name = rawVillage.AllianceName
            };
        }

        public static Player GetPlayer(this RawVillage rawVillage)
        {
            return new Player
            {
                Id = rawVillage.PlayerId,
                Name = rawVillage.PlayerName,
                AllianceId = rawVillage.AllianceId
            };
        }

        public static Village GetVillage(this RawVillage rawVillage)
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

        public static VillagePopulationHistory GetVillagePopulation(this Village village, DateTime date)
        {
            return new VillagePopulationHistory
            {
                VillageId = village.Id,
                Population = village.Population,
                Date = date,
            };
        }

        public static PlayerPopulationHistory GetPlayerPopulation(this Player player, DateTime date)
        {
            return new PlayerPopulationHistory
            {
                PlayerId = player.Id,
                Population = player.Population,
                Date = date,
            };
        }

        public static PlayerAllianceHistory GetPlayerAlliance(this Player player, DateTime date)
        {
            return new PlayerAllianceHistory
            {
                PlayerId = player.Id,
                AllianceId = player.AllianceId,
                Date = date,
            };
        }
    }
}