namespace App.Entities
{
    public class VillageHistory
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public int VillageId { get; set; }
        public int MapId { get; set; }

        public DateOnly SnapshotDate { get; set; }

        public int Population { get; set; }

        public int ChangePopulation { get; set; }
    }
}