using System.Xml.Linq;
using CliWrap;
using CliWrap.Buffered;

namespace LazyDan2.Utils;

public static class StreamUtils
{
    public static XDocument GetNfoFile(Game game, int attempt)
    {
        var title = $"{game.GameTime:MM-dd}-{game.ShortAwayTeam}-{game.ShortHomeTeam}-{attempt:00}";
        var plot = $"{game.AwayTeam} at {game.HomeTeam} on {game.GameTime:yyyy-MM-dd} ({attempt:00})";

        return new XDocument(
            new XElement("episodedetails",
                new XElement("title", title),
                new XElement("showtitle", game.League),
                new XElement("plot", plot),
                new XElement("genre", "Sport"),
                new XElement("aired", game.GameTime.ToString("yyyy-MM-dd")),
                new XElement("season", game.GameTime.ToString("yyyy-MM-dd")),
                new XElement("episode", $"{attempt:00}")
            )
        );
    }

    public static async Task<double> GetDuration(string filePath)
    {
        var result = await Cli.Wrap("ffprobe")
            .WithArguments($"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"")
            .ExecuteBufferedAsync();

        if (double.TryParse(result.StandardOutput.Trim(), out double duration))
        {
            return duration;
        }
        return 0;
    }

    public static async Task RemuxFile(string inputPath, string outputPath)
    {
        await Cli.Wrap("ffmpeg")
            .WithArguments($"-i \"{inputPath}\" -c copy \"{outputPath}\"")
            .ExecuteAsync();
    }
}