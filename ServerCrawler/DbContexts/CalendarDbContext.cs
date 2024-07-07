using Microsoft.EntityFrameworkCore;
using ServerCrawler.Entities;

namespace ServerCrawler.DbContexts
{
    public class CalendarDbContext(string connectionString) : DbContext(GetOptions(connectionString))
    {
        public DbSet<Server> Servers { get; set; }

        private static DbContextOptions<CalendarDbContext> GetOptions(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CalendarDbContext>();
            optionsBuilder
#if DEBUG
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
#endif
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return optionsBuilder.Options;
        }
    }
}