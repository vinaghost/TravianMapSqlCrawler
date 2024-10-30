using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VillageCrawlerCustom;

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var url = configuration.GetValue<string>("url");
if (string.IsNullOrEmpty(url))
{
    Console.WriteLine("Url is required.");
    return;
}

var villages = await DonwloadMapSqlCommand.Handle(url);
if (villages.Count == 0)
{
    Console.WriteLine("No villages found.");
    return;
}

var connectionString = configuration.GetConnectionString("VillageDb");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("Connection string is required.");
    return;
}

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
catch (Exception ex)
{
    await transaction.RollbackAsync();
    Console.WriteLine("An error occurred.");
    Console.WriteLine(ex);
    return;
}

var allianceCount = await context.Alliances.CountAsync();
var playerCount = await context.Players.CountAsync();
var villageCount = await context.Villages.CountAsync();

Console.WriteLine($"Alliances: {allianceCount}");
Console.WriteLine($"Players: {playerCount}");
Console.WriteLine($"Villages: {villageCount}");
Console.WriteLine("Done.");