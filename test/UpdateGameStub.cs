using Hangfire;
using Hangfire.MemoryStorage;
using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

public class UpdateGameStub
{
    private readonly HttpClient httpClient = new HttpClient();
    private readonly GameContext context = new GameContext(new DbContextOptionsBuilder<GameContext>().UseSqlite("Data Source=/home/dan/code/LazyDan2/games.db").Options);
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
    public async Task UpdateMlb()
    {
        var gameService = new GameService(context, httpClient, backgroundJobClient, cache, configuration);

        await gameService.UpdateMlb();
    }

    [Test]
    public async Task UpdateNba()
    {
        var gameService = new GameService(context, httpClient, backgroundJobClient, cache, configuration);

        await gameService.UpdateNba();
    }

    [Test]
    public async Task UpdateNfl()
    {
        var gameService = new GameService(context, httpClient, backgroundJobClient, cache, configuration);

        await gameService.UpdateNfl();
    }

    [Test]
    public async Task UpdateNhl()
    {
        var gameService = new GameService(context, httpClient, backgroundJobClient, cache, configuration);

        await gameService.UpdateNhl();
    }

    [Test]
    public async Task UpdateCfb()
    {
        var gameService = new GameService(context, httpClient, backgroundJobClient, cache, configuration);

        await gameService.UpdateCfb();
    }

    [Test]
    public async Task UpdateEpg()
    {
        var gameService = new GameService(context, httpClient, backgroundJobClient, cache, configuration);

        await gameService.UpdateEpg();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        httpClient.Dispose();
        context.Dispose();
        cache.Dispose();
    }

}