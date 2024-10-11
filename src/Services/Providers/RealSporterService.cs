using System.Text;
using System.Text.RegularExpressions;

namespace LazyDan2.Services.Providers;
public class RealSporterService : IGameStreamProvider
{
    public int Weight { get; } = 5;
    public bool IsEnabled { get; } = false;
    public string Name { get; } = "RealSporter";

    private const string _homeUrl = "https://realsporter.info";
    private const string _originUrl = "https://anarchy-stream.com";
    private readonly HttpClient _httpClient;

    public RealSporterService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_originUrl}/");
        _httpClient.DefaultRequestHeaders.Add("Origin", _originUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<string> GetMlbStream(string team)
    {
        return await GetGameStream(team, "list5-baseball");
    }

    public async Task<string> GetNbaStream(string team)
    {
        return await GetGameStream(team, "list5-basketball");
    }

    public async Task<string> GetNflStream(string team)
    {
        return await GetGameStream(team, "list5-nfl");
    }

    public async Task<string> GetNhlStream(string team)
    {
        return await GetGameStream(team, "list5-hockey");
    }

    public async Task<string> GetCfbStream(string team)
    {
        return await GetGameStream(team, "list5-cfb", isCfb: true);
    }

    public async Task<string> GetWnbaStream(string team)
    {
        return await GetGameStream(team, "list5-wnba");
    }

    private async Task<string> GetGameStream(string team, string league, bool isCfb = false)
    {
        if (isCfb)
            team = team.Replace("State", "St");
        else
            team = team.Split(' ').Last();

        var response = await _httpClient.GetStringAsync($"https://v2.sportsurge.net/{league}");
        var pattern = $"<a href=\"(https://sportsurge.io/watch.+)\"[^>]*>[^<]+?{team}";
        var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase);
        var teamLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(teamLink))
            throw new Exception("Couldn't find team URL");

        response = await _httpClient.GetStringAsync(teamLink);

        match = Regex.Match(response, $"<a href=\"({_homeUrl}[^\"]*)", RegexOptions.IgnoreCase);
        var gameLink = match.Groups[1].Value;

        response = await _httpClient.GetStringAsync(teamLink);

        match = Regex.Match(response, "<iframe.+src=\"(https://[^\"]+)\"");
        var iframeLink = match.Groups[1].Value;

        response = await _httpClient.GetStringAsync(iframeLink);

        // TODO: handle "http://www.sportsbite.cf/p/cfb31.html"
        // Look for <iframe frameborder=0 width=640 height=480 src='//streamer4u.site/live/embed.php?ch=ch49' allowfullscreen scrolling=no allowtransparency></iframe>
        // Note channel changes
        var ch = Regex.Match(gameLink, @"\?ch=([^&]+)&").Groups[1].Value;

        if (string.IsNullOrEmpty(gameLink) || string.IsNullOrEmpty(ch))
            throw new Exception("Couldn't find game URL");

        gameLink = $"https://streamer4u.site/live/embed.php?ch={ch}";

        response = await _httpClient.GetStringAsync(gameLink);

        match = Regex.Match(response, @"window\.atob\(['""]([^'""']*)['""]\)", RegexOptions.IgnoreCase);
        var urlB64 = match.Groups[1].Value;

        if (string.IsNullOrEmpty(urlB64))
            throw new Exception("No URL");

        var url = Encoding.UTF8.GetString(Convert.FromBase64String(urlB64)).Replace("//", "https://");

        return $"/spoof/playlist?url={url}&origin={_originUrl}";
    }
}