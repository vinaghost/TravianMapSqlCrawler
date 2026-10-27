namespace App.Entities
{
    public class Player
    {
        public int Id { get; set; }

        public int AllianceId { get; set; }
        public string Name { get; set; } = "";
        public int Population { get; set; }
        public int VillageCount { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}