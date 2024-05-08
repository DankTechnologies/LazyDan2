using Coravel.Invocable;
using Coravel.Queuing.Interfaces;
using LazyDan2.Services;
using Microsoft.EntityFrameworkCore;

namespace LazyDan2.Jobs;

public class QueueRecordingsJob : IInvocable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IQueue _queue;

    public QueueRecordingsJob(IServiceProvider serviceProvider, IQueue queue)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
    }

    public async Task Invoke()
    {
        using var scope = _serviceProvider.CreateScope();
        var gameService = scope.ServiceProvider.GetRequiredService<GameService>();

        var entries = await gameService.GetDvrEntries().ToListAsync();

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