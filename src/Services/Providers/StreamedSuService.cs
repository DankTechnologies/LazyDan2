using System.Text.RegularExpressions;
using LazyDan2.Types;

namespace LazyDan2.Services.Providers;
public class StreamedSuService : IGameStreamProvider
{
    public int Weight { get; } = 1;
    public bool IsEnabled { get; } = true;
    public string Name { get; } = "StreamedSu";

    private const string _homeUrl = "https://embedme.top";

    private readonly HttpClient _httpClient;

    private readonly GameService _gameService;

    public StreamedSuService(HttpClient httpClient, GameService gameService)
    {
        _httpClient = httpClient;
        _gameService = gameService;
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_homeUrl}/");
        _httpClient.DefaultRequestHeaders.Add("Origin", _homeUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<string> GetCfbStream(string team)
    {
        return await GetGameStream(team, "american-football");
    }

    public async Task<string> GetMlbStream(string team)
    {
        return await GetGameStream(team, "baseball");
    }

    public async Task<string> GetNbaStream(string team)
    {
        return await GetGameStream(team, "basketball");
    }

    public async Task<string> GetNflStream(string team)
    {
        return await GetGameStream(team, "american-football");
    }

    public async Task<string> GetNhlStream(string team)
    {
        return await GetGameStream(team, "hockey");
    }

    public async Task<string> GetWnbaStream(string team)
    {
        return await GetGameStream(team, "basketball");
    }

    private async Task<string> GetGameStream(string team, string league)
    {
        var game = _gameService.GetGames()
            .Where(x => x.State == GameState.InProgress && (x.HomeTeam == team || x.AwayTeam == team))
            .Single();

        var homeTeam = game.HomeTeam.Replace(" ", "-").ToLower();
        var awayTeam = game.AwayTeam.Replace(" ", "-").ToLower();

        // var response = await _httpClient.GetStringAsync($"{_homeUrl}/api/stream/alpha/{homeTeam}-vs-{awayTeam}");
        // https://embedme.top/embed/alpha/new-york-yankees-vs-los-angeles-dodgers/1
        //

        var url = $"https://rr.vipstreams.in/alpha/js/{homeTeam}-vs-{awayTeam}/1/playlist.m3u8";

        // team = team.Split(' ').Last();

        // var match = Regex.Match(response, $"href=\"({_homeUrl}/.+?)\".+{team}", RegexOptions.IgnoreCase);
        // var teamLink = match.Groups[1].Value;

        // if (string.IsNullOrEmpty(teamLink))
        //     throw new Exception("Couldn't find team URL");

        // response = await _httpClient.GetStringAsync(teamLink);
        // match = Regex.Match(response, $@"""(http.+\.m3u8)""", RegexOptions.IgnoreCase);
        // var url = match.Groups[1].Value;

        return $"/spoof/playlist?url={url}&origin={_homeUrl}";
    }
}