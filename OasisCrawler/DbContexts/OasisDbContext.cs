using Microsoft.EntityFrameworkCore;
using OasisCrawler.Entities;

namespace OasisCrawler.DbContexts
{
    public class OasisDbContext(string connectionString, string database) : DbContext(GetOptions(connectionString, database))
    {
        public DbSet<Oasis> Oasises { get; set; }

        private static DbContextOptions<OasisDbContext> GetOptions(string connectionString, string database)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OasisDbContext>();
            optionsBuilder
#if DEBUG
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
#endif
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .UseMySql($"{connectionString};Database={database}", ServerVersion.AutoDetect(connectionString));

            return optionsBuilder.Options;
        }

        public override void Dispose()
        {
            Database.CloseConnection();
            base.Dispose();
        }
    }
}