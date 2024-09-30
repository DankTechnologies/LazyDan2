using System.Text;
using System.Text.RegularExpressions;

namespace LazyDan2.Services.Providers;
public class StreameastService : IGameStreamProvider
{
    public int Weight { get; } = 1;
    public bool IsEnabled { get; } = true;
    public string Name { get; } = "Streameast";

    private const string _homeUrl = "https://www.streameast.gg";

    private readonly HttpClient _httpClient;

    public StreameastService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Referer", $"{_homeUrl}/");
        _httpClient.DefaultRequestHeaders.Add("Origin", _homeUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<string> GetCfbStream(string team)
    {
        return await GetGameStream(team, "ncaastreams");
    }

    public async Task<string> GetMlbStream(string team)
    {
        return await GetGameStream(team, "mlbstreams");
    }

    public async Task<string> GetNbaStream(string team)
    {
        return await GetGameStream(team, "nbastreams");
    }

    public async Task<string> GetNflStream(string team)
    {
        return await GetGameStream(team, "nflstreams");
    }

    public async Task<string> GetNhlStream(string team)
    {
        return await GetGameStream(team, "nhlstreams");
    }

    public async Task<string> GetWnbaStream(string team)
    {
        return await GetGameStream(team, "wnbastreams");
    }

    private async Task<string> GetGameStream(string team, string league)
    {
        var response = await _httpClient.GetStringAsync($"{_homeUrl}/{league}");

        team = team.ToLower().Replace(" ", "-").Replace(".", string.Empty);

        var match = Regex.Match(response, $@"href=""({_homeUrl}/event\/[^""]*{team}[^""]*)""", RegexOptions.IgnoreCase);
        var teamLink = match.Groups[1].Value;

        if (string.IsNullOrEmpty(teamLink))
            throw new Exception("Couldn't find team URL");

        response = await _httpClient.GetStringAsync(teamLink);
        match = Regex.Match(response, @"window\.atob\(['""]([^'""']*)['""]\)", RegexOptions.IgnoreCase);
        var urlB64 = match.Groups[1].Value;

        if (string.IsNullOrEmpty(urlB64))
            throw new Exception("No URL");

        var url = Encoding.UTF8.GetString(Convert.FromBase64String(urlB64));

        return $"/spoof/playlist?url={url}&origin={_homeUrl}";
    }
}