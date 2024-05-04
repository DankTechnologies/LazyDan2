using System.Text;
using System.Xml.Linq;
using LazyDan2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LazyDan2.Controllers;

[ApiController]
[Route("[controller]")]
public class DvrManagementController : ControllerBase
{
    private readonly string _lazyDanUrl;
    private readonly GameService _gameService;
    public DvrManagementController(GameService gameService, IConfiguration configuration)
    {
        _lazyDanUrl = configuration["LazyDanUrl"];
        _gameService = gameService;
    }

    [HttpPost]
    [Route("schedule/{id}")]
    public IActionResult ScheduleDownload(int id)
    {
        var game = _gameService.GetGame(id);

        if (game == null)
            return NotFound();

        if (game.Dvr != null)
            return BadRequest("Game already scheduled for download");

        _gameService.ScheduleDownload(game);
        return Ok();
    }

    [HttpDelete]
    [Route("cancel/{id}")]
    public IActionResult CancelDownload(int id)
    {
        var game = _gameService.GetGame(id);

        if (game == null)
            return NotFound();

        _gameService.CancelDownload(game);
        return Ok();
    }

    [HttpGet]
    public IActionResult GetDvrEntries()
    {
        var ret = _gameService.GetDvrEntries()
            .Include(x => x.Game)
            .ToList();

        return Ok(ret);
    }

    [HttpGet]
    [Route("m3u")]
    public async Task<IActionResult> GetM3U()
    {
        var channels = await _gameService.GetGames()
            .Where(x =>
                !string.IsNullOrEmpty(x.Channel) &&
                x.GameTime > DateTime.Now.AddHours(-8) &&
                x.GameTime < DateTime.Now.AddDays(7)
            )
            .Select(x => x.Channel)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var m3uBuilder = new StringBuilder("#EXTM3U\n");

        foreach (var channel in channels)
        {
            var group = channel.Substring(0, channel.IndexOf('-'));
            m3uBuilder.AppendLine($"#EXTINF:-1 tvg-id=\"{channel}\" tvg-name=\"{channel}\" group-title=\"{group}\",{channel}");
            m3uBuilder.AppendLine($"{_lazyDanUrl}/channel/{channel}");
        }

        return Content(m3uBuilder.ToString(), "audio/x-mpegurl");
    }

    [HttpGet]
    [Route("xmltv")]
    public async Task<IActionResult> GetXmlTv()
    {
        var games = await _gameService.GetGames()
            .Where(x =>
                !string.IsNullOrEmpty(x.Channel) &&
                x.GameTime > DateTime.Now.AddHours(-8) &&
                x.GameTime < DateTime.Now.AddDays(7)
            )
            .ToListAsync();

        var xDoc = new XDocument(new XElement("tv"));

        // Add channels
        var distinctChannels = games.Select(g => g.Channel).Distinct();
        foreach (var channel in distinctChannels)
        {
            xDoc.Root.Add(
                new XElement("channel",
                    new XAttribute("id", channel),
                    new XElement("display-name", channel)
                )
            );
        }

        foreach (var game in games)
        {
            xDoc.Root.Add(
                new XElement("programme",
                    new XAttribute("start", game.GameTime.ToString("yyyyMMddHHmmss")),
                    new XAttribute("stop", game.GameTime.AddHours(4).ToString("yyyyMMddHHmmss")),
                    new XAttribute("channel", game.Channel),
                    new XElement("title", $"{game.AwayTeam} at {game.HomeTeam}"),
                    new XElement("icon",
                        new XAttribute("src", $"{_lazyDanUrl}/poster/{game.League}/{Uri.EscapeDataString(game.HomeTeam)}/{Uri.EscapeDataString(game.AwayTeam)}.png"),
                        new XAttribute("height", "500"),
                        new XAttribute("width", "1000")
                    )
                )
            );
        }

        return Content(xDoc.ToString(), "application/xml");
    }

}
