using Coravel.Invocable;
using LazyDan2.Services;

namespace LazyDan2.Jobs;

public class DownloadGamesJob : IInvocable, IInvocableWithPayload<Game>
{
    public Game Payload { get; set; }

    private readonly IServiceProvider _serviceProvider;

    public DownloadGamesJob(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Invoke()
    {
        using var scope = _serviceProvider.CreateScope();
        var streamService = scope.ServiceProvider.GetRequiredService<StreamService>();

        await streamService.DownloadGame(Payload);
    }
}