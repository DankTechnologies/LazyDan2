using Coravel.Invocable;
using LazyDan2.Services;

namespace LazyDan2.Jobs;

public class DownloadGamesJob : IInvocable, IInvocableWithPayload<Game>
{
    public Game Payload { get; set; }

    private readonly StreamService _streamService;

    public DownloadGamesJob(StreamService streamService)
    {
        _streamService = streamService;
    }

    public async Task Invoke()
    {
        await _streamService.DownloadGame(Payload);
    }
}