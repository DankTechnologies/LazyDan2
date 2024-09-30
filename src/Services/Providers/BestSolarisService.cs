using System.Text.RegularExpressions;

namespace LazyDan2.Services.Providers;
public class BestSolarisService : IGameStreamProvider
{
    public int Weight { get; } = 2;
    public bool IsEnabled { get; } = true;
    public string Name { get; } = "BestSolaris";

    private const string _homeUrl = "https://v1.bestsolaris.com";

    private readonly HttpClient _httpClient;

    public BestSolarisService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_homeUrl}/");
        _httpClient.DefaultRequestHeaders.Add("Origin", _homeUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<string> GetCfbStream(string team)
    {
        return await GetGameStream(team, "cfb");
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

    public async Task<string> GetWnbaStream(string team)
    {
        return await GetGameStream(team, "wnba");
    }

    private async Task<string> GetGameStream(string team, string league)
    {
        var response = await _httpClient.GetStringAsync($"{_homeUrl}/{league}streams");

        team = team.ToLower().Replace(" ", "-").Replace(".", string.Empty);

        var match = Regex.Match(response, $@"<a.*href=""({_homeUrl}\/{league}streams\/[^""]*{team}[^""]*)""", RegexOptions.IgnoreCase);
        var teamLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(teamLink))
            throw new Exception("Couldn't find team URL");

        response = await _httpClient.GetStringAsync(teamLink);
        match = Regex.Match(response, $@"""(http.+\.m3u8)""", RegexOptions.IgnoreCase);
        var url = match.Groups[1].Value;

        return $"/spoof/playlist?url={url}&origin={_homeUrl}";
    }
}