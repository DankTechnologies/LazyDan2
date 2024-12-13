using System.Text;
using System.Text.RegularExpressions;

namespace LazyDan2.Services.Providers;
public class StreameastService : IGameStreamProvider
{
    public int Weight { get; } = 1;
    public bool IsEnabled { get; } = true;
    public string Name { get; } = "Streameast";
    private const string _originUrl = "https://googlapisapi.com";

    private const string _homeUrl = "https://www.streameast.gd";

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

        var teamLink = GetGameUrlByStringMatching(team, response);

        response = await _httpClient.GetStringAsync(teamLink);
        var match = Regex.Match(response, @"window\.atob\(['""]([^'""']*)['""]\)", RegexOptions.IgnoreCase);
        var urlB64 = match.Groups[1].Value;

        if (string.IsNullOrEmpty(urlB64))
            throw new Exception("No URL");

        var url = Encoding.UTF8.GetString(Convert.FromBase64String(urlB64));

        return $"/spoof/playlist?url={url}&origin={_originUrl}";
    }

    private string GetGameUrlByStringMatching(string team, string response)
    {
        // Find the index of the team name
        int teamIndex = response.IndexOf(team, StringComparison.OrdinalIgnoreCase);

        if (teamIndex != -1)
        {
            // Look backwards for the nearest href before the team name
            string substringBeforeTeam = response.Substring(0, teamIndex);
            int hrefIndex = substringBeforeTeam.LastIndexOf("href=\"", StringComparison.OrdinalIgnoreCase);

            if (hrefIndex != -1)
            {
                // Extract the href value
                int hrefStart = hrefIndex + "href=\"".Length;
                int hrefEnd = substringBeforeTeam.IndexOf("\"", hrefStart, StringComparison.OrdinalIgnoreCase);
                string href = substringBeforeTeam.Substring(hrefStart, hrefEnd - hrefStart);

                return href;
            }
            else
            {
                throw new Exception("No href found before the team name.");
            }
        }
        else
        {
            throw new Exception("Team name not found in the response.");
        }
    }
}