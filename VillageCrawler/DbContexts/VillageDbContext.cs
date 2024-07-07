using Microsoft.EntityFrameworkCore;
using VillageCrawler.Entities;

namespace VillageCrawler.DbContexts
{
    public class VillageDbContext(string connectionString, string database) : DbContext(GetOptions(connectionString, database))
    {
        public DbSet<Alliance> Alliances { get; set; }
        public DbSet<AllianceHistory> AlliancesHistory { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerHistory> PlayersHistory { get; set; }
        public DbSet<Village> Villages { get; set; }
        public DbSet<VillageHistory> VillagesHistory { get; set; }

        private static DbContextOptions<VillageDbContext> GetOptions(string connectionString, string database)
        {
            var optionsBuilder = new DbContextOptionsBuilder<VillageDbContext>();
            optionsBuilder
#if DEBUG
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
#endif
                .UseMySql($"{connectionString};Database={database}", ServerVersion.AutoDetect(connectionString));

            return optionsBuilder.Options;
        }
    }
}