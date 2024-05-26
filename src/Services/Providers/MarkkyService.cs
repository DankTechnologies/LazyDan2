using System.Text.RegularExpressions;

namespace LazyDan2.Services.Providers;
public class MarkkyService : IGameStreamProvider
{
    public int Weight { get; } = 5;
    public bool IsEnabled { get; } = true;
    public string Name { get; } = "Markky";

    private const string _homeUrl = "https://v.markkystreams.com";

    private readonly HttpClient _httpClient;

    public MarkkyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_homeUrl}/");
        _httpClient.DefaultRequestHeaders.Add("Origin", _homeUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<string> GetCfbStream(string team)
    {
        return await GetGameStream(team, "ncaaf-streams");
    }

    public async Task<string> GetMlbStream(string team)
    {
        return await GetGameStream(team, "mlb-streams");
    }

    public async Task<string> GetNbaStream(string team)
    {
        return await GetGameStream(team, "video-category/nba-streams");
    }

    public async Task<string> GetNflStream(string team)
    {
        return await GetGameStream(team, "video-category/nfl-streams");
    }

    public async Task<string> GetNhlStream(string team)
    {
        return await GetGameStream(team, "video-category/nhl-streams");
    }

    private async Task<string> GetGameStream(string team, string league)
    {
        var response = await _httpClient.GetStringAsync($"{_homeUrl}/{league}/");

        team = team.Split(' ').Last();

        var match = Regex.Match(response, $"href=\"([^\"]+?)\".+?title=\".*?{team}.*?\"", RegexOptions.IgnoreCase);
        var teamLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(teamLink))
            throw new Exception("Couldn't find team URL");

        response = await _httpClient.GetStringAsync(teamLink);
        match = Regex.Match(response, $@"""(http.+\.m3u8)""", RegexOptions.IgnoreCase);
        var url = match.Groups[1].Value;

        if (string.IsNullOrEmpty(url))
            throw new Exception("Couldn't find game URL");

        return $"/spoof/playlist?url={url}&origin={_homeUrl}";
    }
}