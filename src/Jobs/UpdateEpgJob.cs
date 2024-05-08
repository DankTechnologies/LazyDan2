using Coravel.Invocable;
using LazyDan2.Services;

namespace LazyDan2.Jobs;

public class UpdateEpgJob : IInvocable
{
    private readonly IServiceProvider _serviceProvider;

    public UpdateEpgJob(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Invoke()
    {
        using var scope = _serviceProvider.CreateScope();
        var gameService = scope.ServiceProvider.GetRequiredService<GameService>();
        await gameService.UpdateEpg();
    }
}