using Coravel.Invocable;
using Coravel.Queuing.Interfaces;
using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;

namespace LazyDan2.Jobs;

public class QueueRecordingsJob : IInvocable
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
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
        await _semaphore.WaitAsync();

        try
        {
            var entries = await _gameService.GetDvrEntries().ToListAsync();

            var entriesToRecord = entries
                .Where(x => !x.Started && DateTime.UtcNow > x.Game.GameTime)
                .ToList();

            foreach (var entry in entriesToRecord)
            {
                _logger.LogInformation("Marking recording started for {awayTeam} at {homeTeam}", entry.Game.AwayTeam, entry.Game.HomeTeam);

                entry.Started = true;
                await _gameService.UpdateDownload(entry);

                _queue.QueueInvocableWithPayload<DownloadGamesJob, Game>(entry.Game);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}