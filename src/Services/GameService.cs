using System.Globalization;
using System.Text.Json;
using LazyDan2.Types;
using Microsoft.EntityFrameworkCore;

namespace LazyDan2.Services;

public class GameService
{
    private readonly GameContext _context;
    private readonly HttpClient _httpClient;
    private readonly string _cfbDataToken;
    private static readonly int CurrentYear = DateTime.Now.Year;
    private readonly DateTime StartDate;
    private readonly DateTime EndDate;
    private readonly string DateRange;

    private readonly string _mlbScheduleApi = $"https://statsapi.mlb.com/api/v1/schedule?sportId=1&season={CurrentYear}";
    private static readonly string _cfbScheduleApi = $"https://api.collegefootballdata.com/games?year={CurrentYear}&division=fbs";
    private const string _nbaScheduleApi = $"https://cdn.nba.com/static/json/staticData/scheduleLeagueV2.json";
    private string _nflScheduleApi => $"https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard?dates={DateRange}";
    private string _nhlScheduleApi => $"https://site.api.espn.com/apis/site/v2/sports/hockey/nhl/scoreboard?dates={DateRange}";
    private string _wnbaScheduleApi => $"https://site.api.espn.com/apis/site/v2/sports/basketball/wnba/scoreboard?dates={DateRange}";

    public GameService(GameContext context, HttpClient httpClient, IConfiguration configuration)
    {
        _context = context;
        _httpClient = httpClient;
        _cfbDataToken = configuration["CfbDataToken"];

        StartDate = DateTime.Now.AddDays(-7);
        EndDate = StartDate.AddYears(1);
        DateRange = $"{StartDate:yyyyMMdd}-{EndDate:yyyyMMdd}";
    }

    public async Task<Game> GetGame(int id)
    {
        return await GetGames()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public IQueryable<Game> GetGames()
    {
        return _context.Games;
    }

    public async Task ScheduleDownload(Game game)
    {
        game.DownloadSelected = true;
        await _context.SaveChangesAsync();
    }

    public async Task CancelDownload(Game game)
    {
        game.DownloadSelected = false;
        await _context.SaveChangesAsync();
    }

    public async Task StartDownload(Game game)
    {
        game.DownloadStarted = true;
        await _context.SaveChangesAsync();
    }

    public async Task CompleteDownload(Game game)
    {
        game.DownloadCompleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMlb()
    {
        using var response = await _httpClient.GetAsync(_mlbScheduleApi);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var jsonDoc = await JsonDocument.ParseAsync(stream);

        var root = jsonDoc.RootElement;
        var datesArray = root.GetProperty("dates");

        foreach (var date in datesArray.EnumerateArray())
        {
            var gamesArray = date.GetProperty("games");
            foreach (var game in gamesArray.EnumerateArray())
            {
                var homeTeam = game.GetProperty("teams").GetProperty("home").GetProperty("team").GetProperty("name").GetString().Trim();
                var awayTeam = game.GetProperty("teams").GetProperty("away").GetProperty("team").GetProperty("name").GetString().Trim();
                var gameTime = DateTime.Parse(game.GetProperty("gameDate").GetString(), null, DateTimeStyles.AssumeUniversal);
                var gameType = game.GetProperty("gameType").GetString();
                var startTimeTbd = game.GetProperty("status").GetProperty("startTimeTBD").GetBoolean();
                var state = game.GetProperty("status").GetProperty("detailedState").GetString();

                if (
                    gameTime < DateTime.UtcNow.AddDays(-1) ||
                    startTimeTbd ||
                    gameType == "PR" ||
                    string.IsNullOrWhiteSpace(homeTeam) ||
                    string.IsNullOrWhiteSpace(awayTeam)
                )
                {
                    continue;
                }

                var match = await _context.Games.SingleOrDefaultAsync(g => g.League == League.Mlb && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

                if (match == null)
                {
                    await _context.Games.AddAsync(new Game
                    {
                        League = League.Mlb,
                        HomeTeam = homeTeam,
                        AwayTeam = awayTeam,
                        GameTime = gameTime,
                        State = state
                    });
                }
                else
                {
                    match.State = state;
                    _context.Games.Update(match);
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateNba()
    {
        using var response = await _httpClient.GetAsync(_nbaScheduleApi);
        response.EnsureSuccessStatusCode();

        // Stream the JSON content directly from the response
        using var stream = await response.Content.ReadAsStreamAsync();
        using var jsonDoc = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });

        var root = jsonDoc.RootElement;
        var gameDatesArray = root.GetProperty("leagueSchedule").GetProperty("gameDates");

        foreach (var gameDate in gameDatesArray.EnumerateArray())
        {
            var gamesArray = gameDate.GetProperty("games");
            foreach (var game in gamesArray.EnumerateArray())
            {
                var gameTime = DateTime.Parse(game.GetProperty("gameDateTimeUTC").GetString(), null, DateTimeStyles.AssumeUniversal);

                var homeCity = game.GetProperty("homeTeam").GetProperty("teamCity").GetString();
                var homeName = game.GetProperty("homeTeam").GetProperty("teamName").GetString();
                var awayCity = game.GetProperty("awayTeam").GetProperty("teamCity").GetString();
                var awayName = game.GetProperty("awayTeam").GetProperty("teamName").GetString();

                var homeTeam = $"{homeCity} {homeName}".Trim();
                var awayTeam = $"{awayCity} {awayName}".Trim();

                var status = game.GetProperty("gameStatus").GetInt32();

                var state = status == 3
                ? GameState.Final
                : status == 2
                    ? GameState.InProgress
                    : "Scheduled";

                if (
                    gameTime < DateTime.UtcNow.AddDays(-1) ||
                    string.IsNullOrWhiteSpace(homeTeam) ||
                    string.IsNullOrWhiteSpace(awayTeam)
                )
                {
                    continue;
                }

                var match = await _context.Games.SingleOrDefaultAsync(g => g.League == League.Nba && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

                if (match == null)
                {
                    await _context.Games.AddAsync(new Game
                    {
                        League = League.Nba,
                        HomeTeam = homeTeam,
                        AwayTeam = awayTeam,
                        GameTime = gameTime,
                        State = state
                    });
                }
                else
                {
                    match.State = state;
                    _context.Games.Update(match);
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateNfl()
    {
        using var response = await _httpClient.GetAsync(_nflScheduleApi);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var jsonDoc = await JsonDocument.ParseAsync(stream);

        var root = jsonDoc.RootElement;
        var events = root.GetProperty("events");

        foreach (var game in events.EnumerateArray())
        {
            var teams = game.GetProperty("name").GetString().Split(" at ");
            var awayTeam = teams[0];
            var homeTeam = teams[1];
            var gameTime = DateTime.Parse(game.GetProperty("date").GetString(), null, DateTimeStyles.AssumeUniversal);
            var state = game.GetProperty("status").GetProperty("type").GetProperty("description").GetString();


            if (gameTime < DateTime.UtcNow.AddDays(-1))
            {
                continue;
            }

            var match = await _context.Games.SingleOrDefaultAsync(g => g.League == League.Nfl && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

            if (match == null)
            {
                await _context.Games.AddAsync(new Game
                {
                    League = League.Nfl,
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    GameTime = gameTime,
                    State = state
                });
            }
            else
            {
                match.State = state;
                _context.Games.Update(match);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateNhl()
    {
        using var response = await _httpClient.GetAsync(_nhlScheduleApi);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var jsonDoc = await JsonDocument.ParseAsync(stream);

        var root = jsonDoc.RootElement;
        var events = root.GetProperty("events");

        foreach (var game in events.EnumerateArray())
        {
            var teams = game.GetProperty("name").GetString().Split(" at ");
            var awayTeam = teams[0];
            var homeTeam = teams[1];
            var gameTime = DateTime.Parse(game.GetProperty("date").GetString(), null, DateTimeStyles.AssumeUniversal);
            var state = game.GetProperty("status").GetProperty("type").GetProperty("description").GetString();

            if (gameTime < DateTime.UtcNow.AddDays(-1))
            {
                continue;
            }

            var match = await _context.Games.SingleOrDefaultAsync(g => g.League == League.Nhl && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

            if (match == null)
            {
                await _context.Games.AddAsync(new Game
                {
                    League = League.Nhl,
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    GameTime = gameTime,
                    State = state
                });
            }
            else
            {
                match.State = state;
                _context.Games.Update(match);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateWnba()
    {
        using var response = await _httpClient.GetAsync(_wnbaScheduleApi);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var jsonDoc = await JsonDocument.ParseAsync(stream);

        var root = jsonDoc.RootElement;
        var events = root.GetProperty("events");

        foreach (var game in events.EnumerateArray())
        {
            var teams = game.GetProperty("name").GetString().Split(" at ");
            var awayTeam = teams[0];
            var homeTeam = teams[1];
            var gameTime = DateTime.Parse(game.GetProperty("date").GetString(), null, DateTimeStyles.AssumeUniversal);
            var state = game.GetProperty("status").GetProperty("type").GetProperty("description").GetString();


            if (gameTime < DateTime.UtcNow.AddDays(-1))
            {
                continue;
            }

            var match = await _context.Games.SingleOrDefaultAsync(g => g.League == League.Wnba && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

            if (match == null)
            {
                await _context.Games.AddAsync(new Game
                {
                    League = League.Wnba,
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    GameTime = gameTime,
                    State = state
                });
            }
            else
            {
                match.State = state;
                _context.Games.Update(match);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateCfb()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _cfbScheduleApi);
        request.Headers.Add("Authorization", $"Bearer {_cfbDataToken}");
        request.Headers.Add("accept", "application/json");

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var jsonDoc = await JsonDocument.ParseAsync(stream);

        var gamesArray = jsonDoc.RootElement.EnumerateArray();

        foreach (var game in gamesArray)
        {
            var homeTeam = game.GetProperty("home_team").GetString();
            var awayTeam = game.GetProperty("away_team").GetString();
            var gameTime = DateTime.Parse(game.GetProperty("start_date").GetString(), null, DateTimeStyles.AssumeUniversal);
            var startTimeTbd = game.GetProperty("start_time_tbd").GetBoolean();
            var completed = game.GetProperty("completed").GetBoolean();

            if (gameTime < DateTime.Now.AddDays(-1) || startTimeTbd)
            {
                continue;
            }

            var state = completed
                ? GameState.Final
                : gameTime < DateTime.Now
                    ? GameState.InProgress
                    : "Scheduled";

            var match = await _context.Games.SingleOrDefaultAsync(g => g.League == League.Cfb && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

            if (match == null)
            {
                await _context.Games.AddAsync(new Game
                {
                    League = League.Cfb,
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    GameTime = gameTime,
                    State = state
                });
            }
            else
            {
                match.State = state;
                _context.Games.Update(match);
            }
        }

        await _context.SaveChangesAsync();
    }
}