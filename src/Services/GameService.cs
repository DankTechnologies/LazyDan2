using System.Globalization;
using Hangfire;
using LazyDan2.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace LazyDan2.Services;

public class GameService
{
    private readonly GameContext _context;
    private readonly HttpClient _httpClient;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IMemoryCache _memoryCache;
    private readonly string _cfbDataToken;
    private const string _nhlDateTimeFormat = "yyyy-MM-dd HH:mm:ss 'UTC'";

    private static readonly int CurrentYear = DateTime.Now.Year;
    private static readonly string _mlbScheduleApi = $"https://statsapi.mlb.com/api/v1/schedule?sportId=1&season={CurrentYear}";
    private static readonly string _nbaScheduleApi = $"https://cdn.nba.com/static/json/staticData/scheduleLeagueV2.json";
    private static readonly string _nflScheduleApi = $"https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard?limit=1000&dates={CurrentYear}";
    private static readonly string _nhlScheduleApi = "https://duckduckgo.com/sports.js?q=nhl&league=nhl&type=games&o=json"; // This API doesn't seem to need a year parameter
    private static readonly string _cfbScheduleApi = $"https://api.collegefootballdata.com/games?year={CurrentYear}&division=fbs";

    public GameService(GameContext context, HttpClient httpClient, IBackgroundJobClient backgroundJobClient, IMemoryCache memoryCache, IConfiguration configuration)
    {
        _context = context;
        _httpClient = httpClient;
        _backgroundJobClient = backgroundJobClient;
        _memoryCache = memoryCache;
        _cfbDataToken = configuration["CfbDataToken"];
    }

    public void AddGames(IEnumerable<Game> games)
    {
        _context.Games.AddRange(games);
        _context.SaveChanges();
    }

    public Game GetGame(int id)
    {
        return GetGames()
            .FirstOrDefault(x => x.Id == id);
    }

    public Game GetCurrentGameByChannel(string channel)
    {
        return GetGames()
            .Where(x => x.Channel == channel && x.State == GameState.InProgress)
            .FirstOrDefault();
    }

    public IQueryable<Game> GetGames()
    {
        return _context.Games
            .AsNoTracking()
            .Include(x => x.Dvr);
    }

    public IQueryable<Dvr> GetDvrEntries()
    {
        return _context.Dvrs
            .AsNoTracking()
            .Include(x => x.Game);
    }

    public Dvr ScheduleDownload(Game game)
    {
        var jobId = _backgroundJobClient.Schedule<StreamService>(x => x.DownloadGame(game), DateTime.SpecifyKind(game.GameTime, DateTimeKind.Utc));
        _memoryCache.Set(game.Id, jobId);

        var dvr = new Dvr
        {
            GameId = game.Id,
            Started = false,
            Completed = false
        };

        _context.Dvrs.Add(dvr);
        _context.SaveChanges();
        return dvr;
    }

    public void CancelDownload(Game game)
    {
        if (_memoryCache.TryGetValue(game.Id, out string jobId))
        {
            _backgroundJobClient.Delete(jobId);
        }

        _context.Dvrs.Remove(game.Dvr);
        _context.SaveChanges();
    }

    public Dvr UpdateDownload(Dvr dvr)
    {
        _context.Dvrs.Update(dvr);
        _context.SaveChanges();
        return dvr;
    }

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task UpdateMlb()
    {
        var response = await _httpClient.GetAsync(_mlbScheduleApi);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        var obj = JObject.Parse(json);
        var dates = (JArray)obj["dates"];

        foreach (var date in dates)
        {
            var gameArray = (JArray)date["games"];
            foreach (var game in gameArray)
            {
                var homeTeam = (string)game["teams"]["home"]["team"]["name"];
                var awayTeam = (string)game["teams"]["away"]["team"]["name"];
                var gameTime = DateTime.Parse((string)game["gameDate"], null, DateTimeStyles.AssumeUniversal);
                var gameType = (string)game["gameType"];
                var startTimeTbd = (bool)game["status"]["startTimeTBD"];
                var state = (string)game["status"]["detailedState"];

                if (gameTime < DateTime.UtcNow.AddDays(-1) || startTimeTbd || gameType == "PR")
                {
                    continue;
                }

                var match = _context.Games.SingleOrDefault(g => g.League == League.Mlb && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

                if (match == null)
                {
                    _context.Games.Add(new Game
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

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task UpdateNba()
    {
        var response = await _httpClient.GetAsync(_nbaScheduleApi);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        var obj = JObject.Parse(json);
        var games = obj["leagueSchedule"]["gameDates"]
            .SelectMany(gd => gd["games"])
            .ToList();

        foreach (var game in games)
        {
            var gameTime = DateTime.Parse((string)game["gameDateTimeUTC"], null, DateTimeStyles.AssumeUniversal);

            var homeCity = (string)game["homeTeam"]["teamCity"];
            var homeName = (string)game["homeTeam"]["teamName"];
            var awayCity = (string)game["awayTeam"]["teamCity"];
            var awayName = (string)game["awayTeam"]["teamName"];

            var homeTeam =  $"{homeCity} {homeName}";
            var awayTeam =  $"{awayCity} {awayName}";

            var status = (int)game["gameStatus"];

            var state = status == 3
                ? GameState.Final
                : status == 2
                    ? GameState.InProgress
                    : "Scheduled";

            if (gameTime < DateTime.UtcNow.AddDays(-1))
            {
                continue;
            }

            var match = _context.Games.SingleOrDefault(g => g.League == League.Nba && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

            if (match == null)
            {
                _context.Games.Add(new Game
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

        await _context.SaveChangesAsync();
    }

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task UpdateNfl()
    {
        var response = await _httpClient.GetAsync(_nflScheduleApi);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        var obj = JObject.Parse(json);
        var events = (JArray)obj["events"];

        foreach (var game in events)
        {
            var teams = ((string)game["name"]).Split(" at ");
            var awayTeam = teams[0];
            var homeTeam = teams[1];
            var gameTime = DateTime.Parse((string)game["date"], null, DateTimeStyles.AssumeUniversal);
            var state = (string)game["status"]["type"]["description"];

            // if (gameTime < DateTime.UtcNow.AddDays(-1) || (string)game["season"]["slug"] == "preseason")
            if (gameTime < DateTime.UtcNow.AddDays(-1))
            {
                continue;
            }

            var match = _context.Games.SingleOrDefault(g => g.League == League.Nfl && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

            if (match == null)
            {
                _context.Games.Add(new Game
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

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task UpdateNhl()
    {
        var response = await _httpClient.GetAsync(_nhlScheduleApi);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        var obj = JObject.Parse(json);
        var games = (JArray)obj["data"]["games"];

        foreach (var game in games)
        {
            var homeTeam = (string)game["home_team"]["location"] + " " + (string)game["home_team"]["name"];
            var awayTeam = (string)game["away_team"]["location"] + " " + (string)game["away_team"]["name"];

            var gameTime = DateTimeOffset.ParseExact((string)game["start_time"], _nhlDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).UtcDateTime;
            var state = (string)game["status"];

            state = state == "closed" || state == "complete"
                ? GameState.Final
                : state == "in progress"
                    ? GameState.InProgress
                    : "Scheduled";

            if (gameTime < DateTime.UtcNow.AddDays(-1))
            {
                continue;
            }

            var match = _context.Games.SingleOrDefault(g => g.League == League.Nhl && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

            if (match == null)
            {
                _context.Games.Add(new Game
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

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task UpdateCfb()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _cfbScheduleApi);
        request.Headers.Add("Authorization", $"Bearer {_cfbDataToken}");
        request.Headers.Add("accept", "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        var games = JArray.Parse(json);

        foreach (var game in games)
        {
            var homeTeam = (string)game["home_team"];
            var awayTeam = (string)game["away_team"];
            var gameTime = DateTime.Parse((string)game["start_date"], null, DateTimeStyles.AssumeUniversal);
            var startTimeTbd = (bool)game["start_time_tbd"];
            var completed = (bool)game["completed"];

            if (gameTime < DateTime.Now.AddDays(-1) || startTimeTbd)
            {
                continue;
            }

            var state = completed
                ? GameState.Final
                : gameTime < DateTime.Now
                    ? GameState.InProgress
                    : "Scheduled";

            var match = _context.Games.SingleOrDefault(g => g.League == League.Cfb && g.HomeTeam == homeTeam && g.AwayTeam == awayTeam && g.GameTime == gameTime);

            if (match == null)
            {
                _context.Games.Add(new Game
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

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task UpdateEpg()
    {
        var games = await _context.Games
            .Where(x => x.GameTime > DateTime.Now.AddHours(-8) && x.GameTime < DateTime.Now.AddDays(7))
            .OrderBy(g => g.GameTime)
            .ToListAsync();

        var gameTimesByLeague = new Dictionary<string, List<DateTime>>();

        foreach (var game in games)
        {
            if (!gameTimesByLeague.ContainsKey(game.League))
                gameTimesByLeague[game.League] = new List<DateTime>();

            var trackedGameTimes = gameTimesByLeague[game.League];
            var allocated = false;

            for (int i = 0; i < trackedGameTimes.Count; i++)
            {
                if (trackedGameTimes[i] <= game.GameTime)
                {
                    trackedGameTimes[i] = game.GameTime.AddHours(4);
                    game.Channel = $"{game.League}-{i + 1:00}";
                    allocated = true;
                    break;
                }
            }

            if (!allocated)
            {
                trackedGameTimes.Add(game.GameTime.AddHours(4));
                game.Channel = $"{game.League}-{trackedGameTimes.Count:00}";
            }

            _context.Update(game);
        }

        await _context.SaveChangesAsync();
    }
}