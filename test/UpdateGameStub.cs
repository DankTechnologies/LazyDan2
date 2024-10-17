using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

public class UpdateGameStub
{
    private readonly HttpClient httpClient = new HttpClient();
    private readonly GameContext context = new GameContext(new DbContextOptionsBuilder<GameContext>().UseSqlite("Data Source=/home/dan/code/LazyDan2/games.db").Options);
    private readonly IConfiguration configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true)
        .Build();

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task UpdateMlb()
    {
        var gameService = new GameService(context, httpClient, configuration);

        await gameService.UpdateMlb();
    }

    [Test]
    public async Task UpdateNba()
    {
        var gameService = new GameService(context, httpClient, configuration);

        await gameService.UpdateNba();
    }

    [Test]
    public async Task UpdateNfl()
    {
        var gameService = new GameService(context, httpClient, configuration);

        await gameService.UpdateNfl();
    }

    [Test]
    public async Task UpdateNhl()
    {
        var gameService = new GameService(context, httpClient, configuration);

        await gameService.UpdateNhl();
    }

    [Test]
    public async Task UpdateCfb()
    {
        var gameService = new GameService(context, httpClient, configuration);

        await gameService.UpdateCfb();
    }

    [Test]
    public async Task UpdateWnba()
    {
        var gameService = new GameService(context, httpClient, configuration);

        await gameService.UpdateWnba();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        httpClient.Dispose();
        context.Dispose();
    }

}