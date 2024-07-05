﻿using ConsoleApplication.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConsoleApplication.DbContexts
{
    public class VillageDbContext(string connectionString, string database) : DbContext(GetOptions(connectionString, database))
    {
        public DbSet<Alliance> Alliances { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Village> Villages { get; set; }
        public DbSet<VillagePopulationHistory> VillagePopulationHistory { get; set; }
        public DbSet<PlayerPopulationHistory> PlayerPopulationHistory { get; set; }
        public DbSet<PlayerAllianceHistory> PlayerAllianceHistory { get; set; }

        private static DbContextOptions<ServerDbContext> GetOptions(string connectionString, string database)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ServerDbContext>();
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