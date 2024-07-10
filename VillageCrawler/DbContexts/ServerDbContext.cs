using Microsoft.EntityFrameworkCore;
using VillageCrawler.Entities;

namespace VillageCrawler.DbContexts
{
    public class ServerDbContext(string connectionString) : DbContext(GetOptions(connectionString))
    {
        public DbSet<Server> Servers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Server>()
                .Property(b => b.LastUpdate)
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Server>()
                .Property(b => b.AllianceCount)
                .HasColumnType("int")
                .HasDefaultValueSql("0");

            modelBuilder.Entity<Server>()
                .Property(b => b.PlayerCount)
                .HasColumnType("int")
                .HasDefaultValueSql("0");

            modelBuilder.Entity<Server>()
                .Property(b => b.VillageCount)
                .HasColumnType("int")
                .HasDefaultValueSql("0");
        }

        private static DbContextOptions<ServerDbContext> GetOptions(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ServerDbContext>();
            optionsBuilder
#if DEBUG
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
#endif
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return optionsBuilder.Options;
        }
    }
}