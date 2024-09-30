using System.Text.RegularExpressions;

namespace LazyDan2.Services.Providers;
public class SportsNestService : IGameStreamProvider
{
    public int Weight { get; } = 5;
    public bool IsEnabled { get; } = false;
    public string Name { get; } = "SportsNest";

    private const string _homeUrl = "https://sportsnest.co";
    private readonly HttpClient _httpClient;

    public SportsNestService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_homeUrl}/");
        _httpClient.DefaultRequestHeaders.Add("Origin", _homeUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<string> GetMlbStream(string team)
    {
        return await GetGameStream(team, "mlb");
    }

    public async Task<string> GetNbaStream(string team)
    {
        return await GetGameStream(team, "nba");
    }

    public async Task<string> GetNflStream(string team)
    {
        return await GetGameStream(team, "nfl");
    }

    public async Task<string> GetNhlStream(string team)
    {
        return await GetGameStream(team, "nhl");
    }

    public async Task<string> GetCfbStream(string team)
    {
        return await GetGameStream(team, "nfl", isCfb: true);
    }

    public async Task<string> GetWnbaStream(string team)
    {
        return await GetGameStream(team, "wnba");
    }

    private async Task<string> GetGameStream(string team, string league, bool isCfb = false)
    {
        if (isCfb)
            team = team.Replace("State", "St");
        else
            team = team.Split(' ').Last();

        var response = await _httpClient.GetStringAsync($"https://sportsurge.io/{league}/schedule");

        var pattern = $"<a href=\"(https://sportsurge.io/{league}/event/\\d+)\"[^>]*>[^<]+?{team}";
        var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase);
        var teamLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(teamLink))
            throw new Exception("Couldn't find team URL");

        response = await _httpClient.GetStringAsync(teamLink);

        match = Regex.Match(response, "<a href=\"(https://sportsnest.co/[^\"]*)", RegexOptions.IgnoreCase);
        var gameLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(gameLink))
            throw new Exception("Couldn't find game URL");

        response = await _httpClient.GetStringAsync(gameLink);

        match = Regex.Match(response, @"src: '(https://[^']+)'");

        var url = match.Groups[1].Value;

        if (string.IsNullOrEmpty(url))
            throw new Exception("Couldn't find stream URL");

        return $"/spoof/playlist?url={url}&origin={_homeUrl}";
    }
}