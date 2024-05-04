using System.Text;
using LazyDan2.Services;
using LazyDan2.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LazyDan2.Controllers;

[ApiController]
[Route("[controller]")]
public class SimpleController : ControllerBase
{
    private readonly StreamService _streamService;
    private readonly IMemoryCache _memoryCache;
    private readonly string _lazyDanUrl;
    public SimpleController(StreamService streamService, IConfiguration configuration, IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _streamService = streamService;
        _lazyDanUrl = configuration["LazyDanUrl"];
    }

    [HttpGet]
    [Route("cfb/{team}")]
    public IActionResult GetCfbStream(string team)
    {
        return Content(GetPlaylist(League.Cfb, team), "application/vnd.apple.mpegurl");
    }

    [HttpGet]
    [Route("mlb/{team}")]
    public IActionResult GetMlbStream(string team)
    {
        return Content(GetPlaylist(League.Mlb, team), "application/vnd.apple.mpegurl");
    }

    [HttpGet]
    [Route("nba/{team}")]
    public IActionResult GetNbaStream(string team)
    {
        return Content(GetPlaylist(League.Nba, team), "application/vnd.apple.mpegurl");
    }

    [HttpGet]
    [Route("nfl/{team}")]
    public IActionResult GetNflStream(string team)
    {
        return Content(GetPlaylist(League.Nfl, team), "application/vnd.apple.mpegurl");
    }

    [HttpGet]
    [Route("nhl/{team}")]
    public IActionResult GetNhlStream(string team)
    {
        return Content(GetPlaylist(League.Nhl, team), "application/vnd.apple.mpegurl");
    }

    [HttpGet]
    [Route("redirect/{league}/{team}/{id}")]

    public async Task<IActionResult> StreamRedirect(string league, string team, Guid id)
    {
        _memoryCache.TryGetValue(id, out string originalProvider);

        var gameStream = await _streamService.GetGameStream(league, team, originalProvider);

        if (gameStream.Url == null)
            return NotFound();

        _memoryCache.Set(id, gameStream.Provider, TimeSpan.FromHours(4));

        return Redirect(gameStream.Url);
    }

    private string GetPlaylist(string league, string team)
    {
        var id = Guid.NewGuid().ToString();
        team = Uri.EscapeDataString(team);
        var playlist = new StringBuilder(string.Join('\n',
            "#EXTM3U",
            "#EXT-X-VERSION:3",
            "#EXT-X-STREAM-INF:RESOLUTION=1280x720",
            $"{_lazyDanUrl}/simple/redirect/{league}/{team}/{id}")
        );

        return playlist.ToString();
    }
}
