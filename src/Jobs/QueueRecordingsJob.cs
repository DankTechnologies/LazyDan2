using Coravel.Invocable;
using Coravel.Queuing.Interfaces;
using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;

namespace LazyDan2.Jobs;

public class QueueRecordingsJob : IInvocable
{
    private readonly GameService _gameService;
    private readonly IQueue _queue;

    public QueueRecordingsJob(GameService gameService, IQueue queue)
    {
        _gameService = gameService;
        _queue = queue;
    }

    public async Task Invoke()
    {
        var entries = await _gameService.GetDvrEntries().ToListAsync();

        var gamesToRecord = entries
            .Where(x => !x.Started && DateTime.UtcNow > x.Game.GameTime)
            .Select(x => x.Game)
            .ToList();

        foreach (var game in gamesToRecord)
        {
            _queue.QueueInvocableWithPayload<DownloadGamesJob, Game>(game);
        }
    }
}