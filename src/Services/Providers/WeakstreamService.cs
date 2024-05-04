using System.Text.Json;
using System.Text.RegularExpressions;

namespace LazyDan2.Services.Providers;
public class WeakstreamService : IGameStreamProvider
{
    public int Weight { get; } = 2;
    public bool IsEnabled { get; } = false;
    public string Name { get; } = "Weakstream";

    private const string _homeUrl = "https://weakspell.org";
    private const int _maxAttempts = 5;

    private readonly HttpClient _httpClient;

    public WeakstreamService(HttpClient httpClient)
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
        match = Regex.Match(response, "<a href=\"(https://weak[^\"]*)", RegexOptions.IgnoreCase);
        var gameLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(gameLink))
            throw new Exception("Couldn't find game URL");

        response = await _httpClient.GetStringAsync(gameLink);
        match = Regex.Match(response, @"var vidgstream = ""([^""]+).+gethlsUrl\(vidgstream, (\d+), (\d+)", RegexOptions.Singleline);

        if (!match.Success)
            throw new Exception("Couldn't parse URL fields");

        var vidgstream = match.Groups[1].Value;
        var serverid = int.Parse(match.Groups[2].Value);
        var cid = int.Parse(match.Groups[3].Value);

        var urlLink = $"{_homeUrl}/gethls?idgstream={vidgstream}&serverid={serverid}&cid={cid}";

        response = await _httpClient.GetStringAsync(urlLink);

        var url = JsonSerializer.Deserialize<WeakstreamUrlResponse>(response).rawUrl;

        return $"/spoof/playlist?url={url}&origin={_homeUrl}";
    }

    public class WeakstreamUrlResponse
    {
        public string rawUrl { get; set; }
    }
}