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

        var updateTasks = new List<Task>
        {
            // SafelyUpdate(gameService.UpdateCfb),
            SafelyUpdate(gameService.UpdateMlb),
            SafelyUpdate(gameService.UpdateNba),
            // SafelyUpdate(gameService.UpdateNfl),
            SafelyUpdate(gameService.UpdateNhl)
        };

        await Task.WhenAll(updateTasks);
    }

    private async Task SafelyUpdate(Func<Task> updateFunc)
    {
        try
        {
            await updateFunc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}