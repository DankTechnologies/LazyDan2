using Coravel.Invocable;
using LazyDan2.Services;

namespace LazyDan2.Jobs;

public class UpdateGamesJob : IInvocable
{
    private readonly GameService _gameService;
    private readonly ILogger<UpdateGamesJob> _logger;

    public UpdateGamesJob(GameService gameService, ILogger<UpdateGamesJob> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    public async Task Invoke()
    {
        // TOOD: factor in season start and end dates, for each league

        // await SafelyUpdate(_gameService.UpdateCfb);
        // await SafelyUpdate(gameService.UpdateNfl);
        await SafelyUpdate(_gameService.UpdateMlb);
        await SafelyUpdate(_gameService.UpdateNba);
        await SafelyUpdate(_gameService.UpdateNhl);

        // force GC to keep memory consumption in check
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private async Task SafelyUpdate(Func<Task> updateLeague)
    {
        try
        {
            await updateLeague();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}