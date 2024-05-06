using Coravel.Invocable;
using LazyDan2.Services;

namespace LazyDan2.Jobs;

public class UpdateGamesJob : IInvocable
{
    private readonly GameService _gameService;

    public UpdateGamesJob(GameService gameService)
    {
        _gameService = gameService;
    }

    public async Task Invoke()
    {
        await _gameService.UpdateCfb();
        await _gameService.UpdateMlb();
        await _gameService.UpdateNba();
        await _gameService.UpdateNfl();
        await _gameService.UpdateNhl();
    }
}