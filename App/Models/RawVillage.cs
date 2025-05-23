namespace App.Models
{
    public sealed record RawVillage(
        int MapId,
        int X,
        int Y,
        int Tribe,
        int VillageId,
        string VillageName,
        int PlayerId,
        string PlayerName,
        int AllianceId,
        string AllianceName,
        int Population,
        string Region,
        bool IsCapital,
        bool IsCity,
        bool IsHarbor,
        int VictoryPoints);
}