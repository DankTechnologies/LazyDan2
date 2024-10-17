using LazyDan2.Services;
using Microsoft.AspNetCore.Mvc;

namespace LazyDan2.Controllers;

[ApiController]
[Route("[controller]")]
public class DvrManagementController : ControllerBase
{
    private readonly GameService _gameService;
    public DvrManagementController(GameService gameService)
    {
        _gameService = gameService;
    }

    [HttpPost]
    [Route("schedule/{id}")]
    public async Task<IActionResult> ScheduleDownload(int id)
    {
        var game = await _gameService.GetGame(id);

        if (game == null)
            return NotFound();

        if (game.DownloadSelected)
            return BadRequest("Game already scheduled for download");

        await _gameService.ScheduleDownload(game);
        return Ok();
    }

    [HttpDelete]
    [Route("cancel/{id}")]
    public async Task<IActionResult> CancelDownload(int id)
    {
        var game = await _gameService.GetGame(id);

        if (game == null)
            return NotFound();

        await _gameService.CancelDownload(game);
        return Ok();
    }
}
