using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LazyDan2.Services;

public class PosterService
{
    private readonly ILogger<PosterService> _logger;
    private static readonly string _logoDir = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logos");

    public PosterService(ILogger<PosterService> logger)
    {
        _logger = logger;
    }

    public byte[] CombineLogos(string league, string homeTeam, string awayTeam)
    {
        var proceed = true;

        var awayTeamLogo = GetLogoPath(league, awayTeam);
        var homeTeamLogo = GetLogoPath(league, homeTeam);

        if (awayTeamLogo == null)
        {
            _logger.LogWarning("Couldn't find logo for {team}", awayTeam);
            proceed = false;
        }

        if (homeTeamLogo == null)
        {
            _logger.LogWarning("Couldn't find logo for {team}", homeTeam);
            proceed = false;
        }

        if (!proceed)
            return null;

        using var logoL = Image.Load<Rgba32>(awayTeamLogo);
        using var logoR = Image.Load<Rgba32>(homeTeamLogo);

        var combinedWidth = logoL.Width + logoR.Width;
        var combinedHeight = Math.Max(logoL.Height, logoR.Height);

        using var combinedLogo = new Image<Rgba32>(combinedWidth, combinedHeight);

        combinedLogo.Mutate(x =>
        {
            x.DrawImage(logoL, new Point(0, 0), 1);
            x.DrawImage(logoR, new Point(logoL.Width, 0), 1);
        });

        using var ms = new MemoryStream();
        combinedLogo.SaveAsPng(ms);
        return ms.ToArray();
    }

    public string GetLogoPath(string league, string team)
    {
        team = team.Replace(" ", "_");

        var leagueDir = Path.Combine(_logoDir, league.ToLower());

        var logos = Directory
            .GetFiles(leagueDir, "*.png")
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .ToList();

        var logo = logos
            .FirstOrDefault(x => team.ToLower().Contains(x.ToLower()));

        if (logo == null)
            return null;

        return Path.Combine(leagueDir, $"{logo}.png");
    }
}