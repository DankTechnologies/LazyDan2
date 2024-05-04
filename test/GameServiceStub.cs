using Hangfire;
using Hangfire.MemoryStorage;
using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

public class GameServiceStub
{
    private readonly HttpClient httpClient = new HttpClient();
    private readonly GameContext context = new GameContext(new DbContextOptionsBuilder<GameContext>().UseSqlite("Data Source=/data/lazydan2/games.db").Options);
    private IBackgroundJobClient backgroundJobClient;
    private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

    [SetUp]
    public void Setup()
    {
        GlobalConfiguration.Configuration.UseMemoryStorage();
        backgroundJobClient = new BackgroundJobClient();
    }

    [Test]
    public async Task GetGame()
    {
        var gameService = new GameService(context, httpClient, backgroundJobClient, cache, configuration);

        var game = gameService.GetGame(27);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        httpClient.Dispose();
        context.Dispose();
        cache.Dispose();
    }
}