using VillageCrawler.Entities;
using VillageCrawler.Models;

namespace VillageCrawler.Extensions
{
    public static class RawVillageExtension
    {
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
    }
}