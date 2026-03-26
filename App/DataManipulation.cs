using App.Entities;
using App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static (List<Village> NewVillages, List<Village> OldVillages, List<int> DeletedVillages, List<VillageHistory> HistoryRecords) VillageHistory(List<Village> villages, Dictionary<int, VillageHistory> oldVillageData)
        {
            var orphanedVillages = oldVillageData
                .Where(x => !villages.Select(x => x.Id).Contains(x.Key))
                .Where(x => x.Value.Population != 0)
                .Select(x => new { x.Key, x.Value.Population })
                .ToList();
            var newVillages = villages
                .Where(x => !oldVillageData.ContainsKey(x.Id))
                .ToList();
            var oldVillages = villages
                .Where(x => oldVillageData.ContainsKey(x.Id))
                .ToList();

            var historyToInsert = new List<VillageHistory>();
            foreach (var orphanedVillage in orphanedVillages)
            {
                historyToInsert.Add(new VillageHistory
                {
                    VillageId = orphanedVillage.Key,
                    Date = DateTime.Today,
                    Population = 0,
                    ChangePopulation = -orphanedVillage.Population,
                    PlayerId = oldVillageData[orphanedVillage.Key].PlayerId,
                    ChangePlayer = false
                });
            }
            foreach (var newVillage in newVillages)
            {
                historyToInsert.Add(new VillageHistory
                {
                    VillageId = newVillage.Id,
                    Date = DateTime.Today,
                    Population = newVillage.Population,
                    ChangePopulation = newVillage.Population,
                    PlayerId = newVillage.PlayerId,
                    ChangePlayer = true
                });
            }
            foreach (var oldVillage in oldVillages)
            {
                historyToInsert.Add(new VillageHistory
                {
                    VillageId = oldVillage.Id,
                    Date = DateTime.Today,
                    Population = oldVillage.Population,
                    ChangePopulation = oldVillage.Population - oldVillageData[oldVillage.Id].Population,
                    PlayerId = oldVillage.PlayerId,
                    ChangePlayer = oldVillage.PlayerId != oldVillageData[oldVillage.Id].PlayerId
                });
            }
            var deleteVillages = orphanedVillages.Select(x => x.Key).ToList();
            return (newVillages, oldVillages, deleteVillages, historyToInsert);
        }

        public static (List<Player> NewPlayers, List<Player> OldPlayers, List<int> DeletedPlayers, List<PlayerHistory> HistoryRecords) PlayerHistory(List<Player> players, Dictionary<int, PlayerHistory> oldPlayerData)
        {
            var orphanedPlayers = oldPlayerData
               .Where(x => !players.Select(x => x.Id).Contains(x.Key))
               .Where(x => x.Value.Population != 0)
               .Select(x => new { x.Key, x.Value.Population })
               .ToList();
            var newPlayers = players
                .Where(x => !oldPlayerData.ContainsKey(x.Id))
                .ToList();
            var oldPlayers = players
                .Where(x => oldPlayerData.ContainsKey(x.Id))
                .ToList();

            var historyToInsert = new List<PlayerHistory>();
            foreach (var orphanedPlayer in orphanedPlayers)
            {
                historyToInsert.Add(new PlayerHistory
                {
                    PlayerId = orphanedPlayer.Key,
                    Date = DateTime.Today,
                    Population = 0,
                    ChangePopulation = -orphanedPlayer.Population,
                    ChangeAlliance = false,
                    AllianceId = 0
                });
            }
            foreach (var newPlayer in newPlayers)
            {
                historyToInsert.Add(new PlayerHistory
                {
                    PlayerId = newPlayer.Id,
                    Date = DateTime.Today,
                    Population = newPlayer.Population,
                    ChangePopulation = newPlayer.Population,
                    ChangeAlliance = true,
                    AllianceId = newPlayer.AllianceId
                });
            }
            foreach (var oldPlayer in oldPlayers)
            {
                historyToInsert.Add(new PlayerHistory
                {
                    PlayerId = oldPlayer.Id,
                    Date = DateTime.Today,
                    Population = oldPlayer.Population,
                    ChangePopulation = oldPlayer.Population - oldPlayerData[oldPlayer.Id].Population,
                    AllianceId = oldPlayer.AllianceId,
                    ChangeAlliance = oldPlayer.AllianceId != oldPlayerData[oldPlayer.Id].AllianceId,
                });
            }

            var deletePlayers = orphanedPlayers.Select(x => x.Key).ToList();
            return (newPlayers, oldPlayers, deletePlayers, historyToInsert);
        }

        public static (List<Alliance> NewAlliances, List<Alliance> OldAlliances, List<int> DeletedAlliances, List<AllianceHistory> HistoryRecords) AllianceHistory(List<Alliance> alliances, Dictionary<int, AllianceHistory> oldAllianceData)
        {
            var orphanedAlliances = oldAllianceData
               .Where(x => !alliances.Select(x => x.Id).Contains(x.Key))
               .Where(x => x.Value.PlayerCount != 0)
               .Select(x => new { x.Key, x.Value.PlayerCount })
               .ToList();
            var newAlliances = alliances
                .Where(x => !oldAllianceData.ContainsKey(x.Id))
                .ToList();
            var oldAlliances = alliances
                .Where(x => oldAllianceData.ContainsKey(x.Id))
                .ToList();

            var historyToInsert = new List<AllianceHistory>();
            foreach (var orphanedAlliance in orphanedAlliances)
            {
                historyToInsert.Add(new AllianceHistory
                {
                    AllianceId = orphanedAlliance.Key,
                    Date = DateTime.Today,
                    PlayerCount = 0,
                    ChangePlayerCount = -orphanedAlliance.PlayerCount
                });
            }

            foreach (var newAlliance in newAlliances)
            {
                historyToInsert.Add(new AllianceHistory
                {
                    AllianceId = newAlliance.Id,
                    Date = DateTime.Today,
                    PlayerCount = newAlliance.PlayerCount,
                    ChangePlayerCount = newAlliance.PlayerCount
                });
            }

            foreach (var oldAlliance in oldAlliances)
            {
                historyToInsert.Add(new AllianceHistory
                {
                    AllianceId = oldAlliance.Id,
                    Date = DateTime.Today,
                    PlayerCount = oldAlliance.PlayerCount,
                    ChangePlayerCount = oldAlliance.PlayerCount - oldAllianceData[oldAlliance.Id].PlayerCount
                });
            }
            var deleteAlliances = orphanedAlliances.Select(x => x.Key).ToList();
            return (newAlliances, oldAlliances, deleteAlliances, historyToInsert);
        }
    }
}