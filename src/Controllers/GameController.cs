namespace LazyDan2.Controllers;

using LazyDan2.Services;
using LazyDan2.Types;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

[ApiController]
[Route("[controller]")]
public class GameController : ControllerBase
{
    private const int _maxGames = 200;
    private readonly GameService _gameService;

    public GameController(GameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet]
    [Route("all")]
    public IActionResult GetAllGames([FromQuery] string search)
            {
            var todayUtc = DateTime.Today.ToUniversalTime();

            IQueryable<Game> games = _gameService.GetGames()
                .Where(x => x.GameTime >= todayUtc || x.State == GameState.InProgress)
                .OrderBy(x => x.GameTime);

            if (!string.IsNullOrEmpty(search))
            {
                games = games.Where(game => game.League.ToLower().Contains(search.ToLower())
                                            || game.HomeTeam.ToLower().Contains(search.ToLower())
                                            || game.AwayTeam.ToLower().Contains(search.ToLower()));
            }

            var ret = games.Take(_maxGames).ToList();

            return Ok(ret);
    }

    [HttpGet]
    [Route("{league}")]
    public IActionResult GetLeagueGames(string league, DateTime? startDate, DateTime? endDate)
    {
        var now = DateTime.Now;
        var todayUtc = DateTime.Today.ToUniversalTime();

        startDate ??= todayUtc;
        endDate ??= todayUtc.AddHours(23).AddMinutes(59).AddSeconds(59);

        var games = _gameService.GetGames()
            .Where(x =>
                x.League == league.ToUpper() &&
                (
                    (x.GameTime >= startDate.Value && x.GameTime <= endDate.Value) ||
                    (x.State == GameState.InProgress && x.GameTime > now.AddHours(-8))
                ) &&
                !(league.ToUpper() == League.Cfb && x.State == GameState.Final && x.GameTime < now.AddHours(-4))
            )
            .OrderBy(x => x.GameTime)
            .ToList();

        return Ok(games);
    }
}
