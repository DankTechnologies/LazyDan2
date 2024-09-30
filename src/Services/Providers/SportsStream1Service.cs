using System.Text.RegularExpressions;

namespace LazyDan2.Services.Providers;
public class SportsStream1Service : IGameStreamProvider
{
    public int Weight { get; } = 5;
    public bool IsEnabled { get; } = false;
    public string Name { get; } = "SportsStream1";

    private const string _originUrl = "https://ddolahdplay.xyz";
    private const string _homeUrl = "https://sportstream1.com";
    private readonly HttpClient _httpClient;

    public SportsStream1Service(HttpClient httpClient)
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

        match = Regex.Match(response, $"<a href=\"({_homeUrl}/[^\"]*)", RegexOptions.IgnoreCase);
        var gameLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(gameLink))
            throw new Exception("Couldn't find game URL");

        response = await _httpClient.GetStringAsync(gameLink);

        match = Regex.Match(response, "iframe src=\"(https://[^\"]+)\"");
        var iframeLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(iframeLink))
            throw new Exception("Couldn't find iframe URL");

        response = await _httpClient.GetStringAsync(iframeLink);

        match = Regex.Match(response, "iframe src=\"(https://[^\"]+)\"");
        var iframeLink2 = match.Groups[1].Value;

        if (string.IsNullOrEmpty(iframeLink2))
            throw new Exception("Couldn't find iframe2 URL");

        var iframeHost = "https://" + new Uri(iframeLink).Host;
        var hc = new HttpClient();
        hc.DefaultRequestHeaders.Add("Referer", $"{iframeHost}/");
        hc.DefaultRequestHeaders.Add("Origin", iframeHost);
        hc.DefaultRequestHeaders.Add("User-Agent", _httpClient.DefaultRequestHeaders.UserAgent.ToString());

        response = await hc.GetStringAsync(iframeLink2);

        match = Regex.Match(response, @"source:\s*'(https://[^']+)'");

        var url = match.Groups[1].Value;
        return $"/spoof/playlist?url={url}&origin={_originUrl}";
    }
}