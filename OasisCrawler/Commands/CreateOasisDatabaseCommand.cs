using MediatR;
using Microsoft.Extensions.Options;
using MySqlConnector;
using OasisCrawler.DbContexts;
using OasisCrawler.Models.Options;

namespace OasisCrawler.Commands
{
    public record CreateOasisDatabaseCommand(string Url) : IRequest<OasisDbContext>;

    public class CreateOasisDatabaseCommandHandler(IOptions<ConnectionStrings> connections)
        : IRequestHandler<CreateOasisDatabaseCommand, OasisDbContext>
    {
        private readonly ConnectionStrings _connections = connections.Value;

        public async Task<OasisDbContext> Handle(CreateOasisDatabaseCommand request, CancellationToken cancellationToken)
        {
            await CreateTable(_connections.Oasis, request.Url, cancellationToken);
            var context = new OasisDbContext(_connections.Oasis, request.Url);
            return context;
        }

        private async Task CreateTable(string connectionString, string url, CancellationToken cancellationToken)
        {
            using var connection = new MySqlConnection($"{connectionString};Database={url}");
            await connection.OpenAsync(cancellationToken);

            var commandText = """
CREATE TABLE IF NOT EXISTS "Oasises" (
    "Id" int NOT NULL,
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
    }
}