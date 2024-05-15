using System.Diagnostics;
using LazyDan2.Utils;
using NUnit.Framework;

public class StreamUtilsStub
{
    [Test]
    public async Task GetNfoXml()
    {
        var game = new Game
        {
            AwayTeam = "Chicago Cubs",
            HomeTeam = "St. Louis Cardinals",
            GameTime = DateTime.Now,
            League = "MLB"
        };

        var attempt = 1;

        var nfoXml = StreamUtils.GetNfoFile(game, attempt);

        Debug.WriteLine(nfoXml.ToString());
    }

    [Test]
    public async Task GetDurationTest()
    {
        var badFile = "/home/dan/scratch/bad.ts";
        var goodFile = "/home/dan/scratch/good.ts";

        var badDuration = await StreamUtils.GetDuration(badFile);
        var goodDuration = await StreamUtils.GetDuration(goodFile);

        Assert.That(badDuration, Is.GreaterThan(86400));
        Assert.That(goodDuration, Is.LessThan(86400));
    }

    [Test]
    public async Task BrokenTsWorkaroundTest()
    {
        var fixedFile = Path.GetTempFileName() + ".ts";

        try
        {
            var badFile = "/home/dan/scratch/bad.ts";
            await StreamUtils.RemuxFile(badFile, fixedFile);

            var badDuration = await StreamUtils.GetDuration(badFile);
            var fixedDuration = await StreamUtils.GetDuration(fixedFile);

            Assert.That(badDuration, Is.GreaterThan(86400));
            Assert.That(fixedDuration, Is.LessThan(86400));
        }
        finally
        {
            if (File.Exists(fixedFile))
                File.Delete(fixedFile);
        }
    }
}
