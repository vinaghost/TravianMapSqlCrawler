using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MySqlConnector;
using OasisCrawler.DbContexts;
using VillageCrawler.Models.Options;

namespace OasisCrawler
{
    public sealed class StartUp(
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<ConnectionStrings> connections)
        : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;
        private readonly ConnectionStrings _connections = connections.Value;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await CreateTable(cancellationToken);

            using var context = new OasisDbContext(_connections.Oasis, "OasisTest");
            context.Oasises.Count();

            await Task.CompletedTask;
            _hostApplicationLifetime.StopApplication();
        }

        private async Task CreateTable(CancellationToken cancellationToken)
        {
            using var connection = new MySqlConnection($"{_connections.Oasis};Database=OasisTest");
            await connection.OpenAsync(cancellationToken);

            var commandText = """
CREATE TABLE IF NOT EXISTS "Oasises" (
    "Id" int NOT NULL AUTO_INCREMENT,
    "X" int NOT NULL,
    "Y" int NOT NULL,
    "Type" int NOT NULL,
    "Detail" int NOT NULL,
    PRIMARY KEY ("Id"),
    KEY "IX_Oasises_Type_Detail" ("Type","Detail"),
    KEY "IX_Oasises_X_Y_Type_Detail" ("X","Y","Type","Detail")
);
""";

            using var command = new MySqlCommand(commandText, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}