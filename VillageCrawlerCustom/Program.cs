using Microsoft.Extensions.Configuration;
using VillageCrawlerCustom;

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var url = configuration.GetValue<string>("Url");
if (string.IsNullOrEmpty(url)) return;

var villages = await DonwloadMapSqlCommand.Handle(url);
if (villages.Count == 0) return;

var connectionString = configuration.GetConnectionString("VillageDb");
if (string.IsNullOrEmpty(connectionString)) return;

using var context = new VillageDbContext(connectionString, url);
await context.Database.EnsureCreatedAsync();

var transaction = await context.Database.BeginTransactionAsync();
try
{
    await context.UpdateAlliance(villages);
    await context.UpdatePlayer(villages);
    await context.UpdateVillage(villages);
    await transaction.CommitAsync();
}
catch (Exception)
{
    await transaction.RollbackAsync();
}