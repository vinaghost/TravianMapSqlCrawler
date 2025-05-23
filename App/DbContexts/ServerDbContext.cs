using App.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.DbContexts
{
    public class ServerDbContext(string connectionString) : DbContext(GetOptions(connectionString))
    {
        public DbSet<Server> Servers { get; set; }

        private static DbContextOptions<ServerDbContext> GetOptions(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ServerDbContext>();
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