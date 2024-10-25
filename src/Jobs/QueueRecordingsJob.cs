using Coravel.Invocable;
using Coravel.Queuing.Interfaces;
using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;

namespace LazyDan2.Jobs;

public class QueueRecordingsJob : IInvocable
{
    private readonly GameService _gameService;
    private readonly IQueue _queue;

    private readonly ILogger<QueueRecordingsJob> _logger;

    public QueueRecordingsJob(ILogger<QueueRecordingsJob> logger, GameService gameService, IQueue queue)
    {
        _logger = logger;
        _gameService = gameService;
        _queue = queue;
    }

    public async Task Invoke()
    {
        var games = await _gameService.GetGames()
            .Where(x => x.DownloadSelected && !x.DownloadStarted && DateTime.UtcNow > x.GameTime)
            .ToListAsync();

        foreach (var game in games)
        {
            _logger.LogInformation("Starting download for {awayTeam} at {homeTeam}", game.AwayTeam, game.HomeTeam);
            await _gameService.StartDownload(game);

            _queue.QueueInvocableWithPayload<DownloadGamesJob, Game>(game);
        }
    }
}