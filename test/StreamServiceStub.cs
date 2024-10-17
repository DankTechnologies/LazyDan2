using System.Reflection;
using LazyDan2.Services;
using LazyDan2.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

public class StreamServiceStub
{
    private readonly ILogger<StreamService> logger = new LoggerFactory().CreateLogger<StreamService>();
    private readonly ILogger<PosterService> posterLogger = new LoggerFactory().CreateLogger<PosterService>();
    private readonly HttpClient httpClient = new HttpClient();
    private readonly GameContext context = new GameContext(new DbContextOptionsBuilder<GameContext>().UseSqlite("Data Source=/data/lazydan2/games.db").Options);
    private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

    private readonly string lazyDanUrl = "https://lazy.pitpat.me";

    [Test]
    public async Task GetGameStreamStub()
    {
        var league = League.Cfb;
        var team = "Arizona State";

        var gameStreamProviderTypes = Assembly.Load("LazyDan2").GetTypes()
            .Where(t => typeof(IGameStreamProvider).IsAssignableFrom(t) && !t.IsInterface)
            .ToList();


        // Create instances of those types
        var gameStreamProviders = gameStreamProviderTypes
            .Select(type => (IGameStreamProvider)Activator.CreateInstance(type, new HttpClient()))
            .ToList();

        var gameService = new GameService(context, httpClient, configuration);
        var posterService = new PosterService(posterLogger);
        var streamService = new StreamService(logger, gameStreamProviders, configuration, gameService, httpClient);

        var result = await streamService.GetGameStream(league, team);
        Console.WriteLine("Result: " + result.Url);
        Console.WriteLine("Provider: " + result.Provider);
    }

    [Test]
    public async Task GetGameStreamFromSpecificProviderStub()
    {
        var providerName = "MethStreamsService";
        var league = League.Nfl;
        var team = "New York Jets";

        var url = await GetGameStreamFromSpecificProvider(providerName, league, team);
        Console.WriteLine(url);
    }

    [Test]
    public async Task GameStreamReportCard()
    {
        var providerNames = Assembly.Load("LazyDan2").GetTypes()
            .Where(t => typeof(IGameStreamProvider).IsAssignableFrom(t) && !t.IsInterface)
            .Select(x => x.Name);

        var league = League.Nfl;
        var team = "New York Jets";

        foreach (var providerName in providerNames)
        {
            try
            {
                var url = await GetGameStreamFromSpecificProvider(providerName, league, team);
                Console.WriteLine($"{providerName} - OK - {url}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{providerName} - {ex.Message}");
            }
        }
    }

    private async Task<string> GetGameStreamFromSpecificProvider(string providerName, string league, string team)
    {
        var provider = Assembly.Load("LazyDan2").GetTypes()
            .Where(t => typeof(IGameStreamProvider).IsAssignableFrom(t) && !t.IsInterface)
            .FirstOrDefault(x => x.Name == providerName);

        object[] constructorArgs = { new HttpClient() };
        var instance = (IGameStreamProvider)Activator.CreateInstance(provider, constructorArgs);

        if (!instance.IsEnabled)
            throw new Exception("Disabled");

        string spoofUrl = null;
        spoofUrl = league switch
        {
            League.Mlb => await instance.GetMlbStream(team),
            League.Nba => await instance.GetNbaStream(team),
            League.Nfl => await instance.GetNflStream(team),
            League.Nhl => await instance.GetNhlStream(team),
            League.Cfb => await instance.GetCfbStream(team),
            League.Wnba => await instance.GetWnbaStream(team),
            _ => throw new Exception("Invalid league"),
        };
        var response = await httpClient.GetAsync($"{lazyDanUrl}{spoofUrl}");
        response.EnsureSuccessStatusCode();

        return spoofUrl;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        httpClient.Dispose();
        context.Dispose();
        cache.Dispose();
    }

}