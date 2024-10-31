using System.Text.RegularExpressions;

namespace LazyDan2.Services.Providers;
public class MethStreamsService : IGameStreamProvider
{
    public int Weight { get; } = 1;
    public bool IsEnabled { get; } = false;
    public string Name { get; } = "MethStreams";

    private const string _originUrl = "https://v1.bestsolaris.com";
    private const string _homeUrl = "https://pre.methstreams.me";

    private readonly HttpClient _httpClient;

    public MethStreamsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_originUrl}/");
        _httpClient.DefaultRequestHeaders.Add("Origin", _originUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<string> GetCfbStream(string team)
    {
        return await GetGameStream(team, "ncaa-streams");
    }

    public async Task<string> GetMlbStream(string team)
    {
        return await GetGameStream(team, "mlb-streams");
    }

    public async Task<string> GetNbaStream(string team)
    {
        return await GetGameStream(team, "nba-streams");
    }

    public async Task<string> GetNflStream(string team)
    {
        return await GetGameStream(team, "nfl-streams");
    }

    public async Task<string> GetNhlStream(string team)
    {
        return await GetGameStream(team, "nhl-streams");
    }

    public async Task<string> GetWnbaStream(string team)
    {
        return await GetGameStream(team, "wnba-streams");
    }

    private async Task<string> GetGameStream(string team, string league)
    {
        var response = await _httpClient.GetStringAsync($"{_homeUrl}/{league}");

        team = team.ToLower().Replace(" ", "-").Replace(".", string.Empty);

        var match = Regex.Match(response, $@"href=""({_homeUrl}/match\/[^""]*{team}[^""]*)""", RegexOptions.IgnoreCase);
        var teamLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(teamLink))
            throw new Exception("Couldn't find team URL");

        response = await _httpClient.GetStringAsync(teamLink);

        match = Regex.Match(response, $@"src=""(https://v1.bestsolaris.com[^""]+)""", RegexOptions.IgnoreCase);
        var embedLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(embedLink))
            throw new Exception("Couldn't find embedded stream URL");

        response = await _httpClient.GetStringAsync(embedLink);

        match = Regex.Match(response, $@"""(http.+\.m3u8)""", RegexOptions.IgnoreCase);
        var url = match.Groups[1].Value;

        return $"/spoof/playlist?url={url}&origin={_originUrl}";
    }
}