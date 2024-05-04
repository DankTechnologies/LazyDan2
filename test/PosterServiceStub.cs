using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

public class PosterServiceStub
{
    private readonly GameContext _context = new GameContext(new DbContextOptionsBuilder<GameContext>().UseSqlite("Data Source=/data/lazydan2/games.db").Options);

    private readonly ILogger<PosterService> _logger = new LoggerFactory().CreateLogger<PosterService>();

    [Test]
    public async Task GetLogoPathStub()
    {
        var allGood = true;

        var posterService = new PosterService(_logger);

        var teams = _context.Games
            .Select(x => new { x.League, Team = x.HomeTeam })
            .Distinct()
            .ToList();

        foreach (var team in teams)
        {
            var logoPath = posterService.GetLogoPath(team.League, team.Team);

            if (logoPath == null || !File.Exists(logoPath))
            {
                Console.WriteLine($"Baddie => {team.League} {team.Team} - {logoPath}");
                allGood = false;
            }
        }

        Assert.That(allGood);
    }

    [Test]
    public async Task CombineLogosStub()
    {
        var posterService = new PosterService(_logger);

        var homeTeam = "Chicago Cubs";
        var awayTeam = "Arizona Diamondbacks";
        var outputPath = "/home/dan/code/LazyDan2/scratch/poster.png";

        var logo = posterService.CombineLogos("mlb", homeTeam, awayTeam);

        Assert.That(logo != null);

        File.WriteAllBytes(outputPath, logo);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

}