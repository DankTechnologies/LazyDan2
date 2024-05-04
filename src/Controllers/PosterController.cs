using LazyDan2.Services;
using Microsoft.AspNetCore.Mvc;

namespace LazyDan2.Controllers;

[ApiController]
[Route("[controller]")]
public class PosterController : ControllerBase
{
    private readonly PosterService _posterService;
    public PosterController(PosterService posterService)
    {
        _posterService = posterService;
    }

    [HttpGet]
    [Route("{league}/{homeTeam}/{awayTeam}")]
    [Route("{league}/{homeTeam}/{awayTeam}.png")]
    public IActionResult Get(string league, string homeTeam, string awayTeam)
    {
        var logo = _posterService.CombineLogos(league, homeTeam, awayTeam);

        if (logo == null)
            return NotFound();

        return File(logo, "image/png");
    }
}
