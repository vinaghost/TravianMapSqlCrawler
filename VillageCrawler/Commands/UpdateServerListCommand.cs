using MediatR;
using Microsoft.Extensions.Options;
using VillageCrawler.DbContexts;
using VillageCrawler.Entities;
using VillageCrawler.Models.Options;

namespace VillageCrawler.Commands
{
    public record UpdateServerListCommand(IList<Server> Servers) : IRequest;

    public class UpdateServerListCommandHandler(IOptions<ConnectionStrings> connections) : IRequestHandler<UpdateServerListCommand>
    {
        private readonly ConnectionStrings _connections = connections.Value;

        public async Task Handle(UpdateServerListCommand request, CancellationToken cancellationToken)
        {
            using var context = new ServerDbContext(_connections.Server);
            await context.Database.EnsureCreatedAsync(cancellationToken);
            await context.BulkUpdateAsync(request.Servers);
        }
    }
}