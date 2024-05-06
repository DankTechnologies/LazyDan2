using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

public class GameServiceStub
{
    private readonly HttpClient httpClient = new HttpClient();
    private readonly GameContext context = new GameContext(new DbContextOptionsBuilder<GameContext>().UseSqlite("Data Source=/data/lazydan2/games.db").Options);
    private readonly IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task GetGame()
    {
        var gameService = new GameService(context, httpClient, configuration);

        var game = gameService.GetGame(27);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        httpClient.Dispose();
        context.Dispose();
    }
}