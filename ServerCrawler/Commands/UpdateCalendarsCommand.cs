using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServerCrawler.DbContexts;
using ServerCrawler.Entities;
using ServerCrawler.Models;
using ServerCrawler.Models.Options;

namespace ServerCrawler.Commands
{
    public record UpdateCalendarsCommand(IList<RawServer> Servers) : IRequest;

    public class UpdateCalendarsCommandHandler(IOptions<ConnectionStrings> connections) : IRequestHandler<UpdateCalendarsCommand>
    {
        private readonly ConnectionStrings _connections = connections.Value;

        public async Task Handle(UpdateCalendarsCommand request, CancellationToken cancellationToken)
        {
            using var context = new CalendarDbContext(_connections.Calendar);
            await context.Database.EnsureCreatedAsync(cancellationToken);

            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            await context.Servers
                .Where(x => x.StartDate <= DateTime.Now.AddDays(-7))
                .ExecuteDeleteAsync(cancellationToken);

            var dbServers = await context.Servers
                .Select(x => x.Name)
                .ToListAsync(cancellationToken: cancellationToken);

            var newServers = request.Servers
                .Where(x => !dbServers.Contains(x.Name))
                .Select(x => new Server
                {
                    Name = x.Name,
                    StartDate = x.StartDate,
                })
                .ToList();

            await context.Servers.AddRangeAsync(newServers, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
    }
}