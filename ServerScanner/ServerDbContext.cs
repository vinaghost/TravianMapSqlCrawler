using Microsoft.EntityFrameworkCore;
using ServerScanner.Entities;

namespace ServerScanner
{
    public class ServerDbContext(string connectionString) : DbContext(GetOptions(connectionString))
    {
        public DbSet<LobbyServer> LobbyServers { get; set; }

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