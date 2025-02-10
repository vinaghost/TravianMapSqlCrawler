using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VillageCrawler.DbContexts;
using VillageCrawler.Models.Options;

namespace VillageCrawler.Commands
{
    public record CreateVillageDatabaseCommand(string Url) : IRequest<VillageDbContext>;

    public class CreateVillageDatabaseCommandHandler(IOptions<ConnectionStrings> connections)
        : IRequestHandler<CreateVillageDatabaseCommand, VillageDbContext>
    {
        private readonly ConnectionStrings _connections = connections.Value;

        public async Task<VillageDbContext> Handle(CreateVillageDatabaseCommand request, CancellationToken cancellationToken)
        {
            var context = new VillageDbContext(_connections.Server, request.Url);
            await context.Database.EnsureCreatedAsync(cancellationToken);

            await context.Database.ExecuteSqlRawAsync("SET GLOBAL local_infile = true;", cancellationToken);
            return context;
        }
    }
}