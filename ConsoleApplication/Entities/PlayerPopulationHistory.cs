using Microsoft.EntityFrameworkCore;

namespace ConsoleApplication.Entities
{
    [Index(nameof(PlayerId), nameof(Date))]
    public class PlayerPopulationHistory
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }

        public DateTime Date { get; set; }

        public int Population { get; set; }

        public int Change { get; set; }
    }
}