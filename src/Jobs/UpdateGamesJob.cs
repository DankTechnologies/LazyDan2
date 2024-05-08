using Coravel.Invocable;
using LazyDan2.Services;

namespace LazyDan2.Jobs;

public class UpdateGamesJob : IInvocable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UpdateGamesJob> _logger;

    public UpdateGamesJob(IServiceProvider serviceProvider, ILogger<UpdateGamesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Invoke()
    {
        using var scope = _serviceProvider.CreateScope();
        var gameService = scope.ServiceProvider.GetRequiredService<GameService>();

        // await SafelyUpdate(gameService.UpdateCfb);
        await SafelyUpdate(gameService.UpdateMlb);
        await SafelyUpdate(gameService.UpdateNba);
        // await SafelyUpdate(gameService.UpdateNfl);
        await SafelyUpdate(gameService.UpdateNhl);
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