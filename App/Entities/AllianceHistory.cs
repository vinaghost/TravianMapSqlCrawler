namespace App.Entities
{
    public class AllianceHistory
    {
        public int Id { get; set; }

        public int AllianceId { get; set; }

        public DateOnly SnapshotDate { get; set; }

        public int PlayerCount { get; set; }
        public int ChangePlayerCount { get; set; }
    }
}