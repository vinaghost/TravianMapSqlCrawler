using Microsoft.EntityFrameworkCore;

namespace VillageCrawler.Entities
{
    [Index(nameof(PlayerId), nameof(Date))]
    [Index(nameof(PlayerId), nameof(ChangePopulation))]
    [Index(nameof(PlayerId), nameof(ChangeAlliance))]
    public class PlayerHistory
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }

        public DateTime Date { get; set; }

        public int AllianceId { get; set; }

        public bool ChangeAlliance { get; set; }

        public int Population { get; set; }

        public bool ChangePopulation { get; set; }
    }
}