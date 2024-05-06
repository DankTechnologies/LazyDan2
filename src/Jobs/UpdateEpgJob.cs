using Coravel.Invocable;
using LazyDan2.Services;

namespace LazyDan2.Jobs;

public class UpdateEpgJob : IInvocable
{
    private readonly GameService _gameService;

    public UpdateEpgJob(GameService gameService)
    {
        _gameService = gameService;
    }

    public async Task Invoke()
    {
        await _gameService.UpdateEpg();
    }
}