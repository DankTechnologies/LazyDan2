namespace LazyDan2.Services;

using System.Diagnostics;
using System.Text;
using CliWrap;
using CliWrap.Buffered;
using LazyDan2.Types;
using LazyDan2.Utils;

public class StreamService
{
    private readonly ILogger<StreamService> _logger;
    private readonly GameService _gameService;
    private readonly PosterService _posterService;
    private readonly IEnumerable<IGameStreamProvider> _providers;
    private readonly HttpClient _client;
    private readonly string _downloadPath;
    private readonly string _lazyDanLanUrl;
    private readonly string _lazyDanUrl;
    private readonly string _pushbulletAccessToken;
    private readonly string _plexUrl;
    private readonly string _plexAccessToken;
    private readonly string _jellyfinAccessToken;
    private readonly string _jellyfinUrl;
    private const int _maxAttempts = 100;
    private const int _gameHoursMin = 2;
    private const string _streamlinkArgs =
        "-f " +
        "--hls-live-edge 12 " +
        "--retry-open 5 " +
        "--ringbuffer-size 256M ";
    public StreamService(ILogger<StreamService> logger, IEnumerable<IGameStreamProvider> providers, IConfiguration configuration, GameService gameService, HttpClient client, PosterService posterService)
    {
        _logger = logger;
        _providers = providers;
        _downloadPath = configuration.GetValue<string>("DownloadPath");
        _pushbulletAccessToken = configuration.GetValue<string>("PushbulletAccessToken");
        _plexAccessToken = configuration.GetValue<string>("PlexAccessToken");
        _plexUrl = configuration.GetValue<string>("PlexUrl");
        _jellyfinAccessToken = configuration.GetValue<string>("JellyfinAccessToken");
        _jellyfinUrl = configuration.GetValue<string>("JellyfinUrl");
        _lazyDanUrl = configuration.GetValue<string>("LazyDanUrl");
        _lazyDanLanUrl = configuration.GetValue<string>("LazyDanLanUrl");
        _gameService = gameService;
        _posterService = posterService;
        _client = client;
    }

    public async Task<(string Url, string Provider)> GetGameStream(string league, string team, string forceProvider = null)
    {
        if (forceProvider != null)
        {
            var provider = _providers.First(x => x.Name == forceProvider);
            var url = await GetStreamFromProvider(provider, league, team);
            await SanityTestStream(url);
            return (Url: url, Provider: provider.Name);
        }

        var providerTasks = _providers
            .Where(x => x.IsEnabled)
            .Select(provider => GetStreamWithTimeout(provider, league, team));

        var results = await Task.WhenAll(providerTasks);

        var validResults = results
            .Where(x => x.Url != null)
            .ToList();

        if (validResults.Count == 0)
        {
            throw new Exception($"No stream found for {league} {team}");
        }

        _logger.LogInformation("Found {count} valid streams for {league} {team}", validResults.Count, league, team);
        _logger.LogInformation("Valid streams: {streams}", string.Join(", ", validResults.Select(x => x.Provider)));

        return SelectRandomStream(validResults, league, team);
    }

    private async Task<(string Url, string Provider)> GetStreamWithTimeout(IGameStreamProvider provider, string league, string team)
    {
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));

        try
        {
            _logger.LogInformation("Getting stream from {provider} for {league} {team}", provider.Name, league, team);

            var urlTask = GetStreamFromProvider(provider, league, team);
            var completedTask = await Task.WhenAny(urlTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogInformation("Timed out getting stream from {provider} for {league} {team}", provider.Name, league, team);
                return (Url: null, Provider: provider.Name);
            }

            var url = await urlTask;
            await SanityTestStream(url);

            _logger.LogInformation("Successful getting stream from {provider} for {league} {team}", provider.Name, league, team);

            return (Url: url, Provider: provider.Name);
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Error getting stream from {Provider} for {League} {Team}", provider.Name, league, team);
            return (Url: null, Provider: provider.Name);
        }
    }

    private (string Url, string Provider) SelectRandomStream(List<(string Url, string Provider)> validResults, string league, string team)
    {
        var totalWeight = validResults
            .Sum(result => _providers.First(x => x.Name == result.Provider).Weight);

        var randomWeightPoint = new Random().Next(0, totalWeight);

        foreach (var result in validResults)
        {
            randomWeightPoint -= _providers.First(x => x.Name == result.Provider).Weight;

            if (randomWeightPoint < 0)
            {
                _logger.LogInformation("Selected {provider} for {league} {team}", result.Provider, league, team);
                return result;
            }
        }

        throw new Exception($"No stream found for {league} {team}");
    }

    public async Task<(string Url, string Provider)> GetGameStream(string channel)
    {
        var game = await _gameService.GetCurrentGameByChannel(channel) ?? throw new Exception("No game found");
        return await GetGameStream(game.League, game.HomeTeam);
    }

    public async Task DownloadGame(Game game)
    {
        _logger.LogInformation("Downloading {awayTeam} at {homeTeam}", game.AwayTeam, game.HomeTeam);

        var swGame = new Stopwatch();
        swGame.Start();

        var dvrEntry = _gameService.GetDvrEntries().FirstOrDefault(x => x.Game.Id == game.Id);

        if (dvrEntry == null)
            return;

        dvrEntry.Started = true;
        await _gameService.UpdateDownload(dvrEntry);

        var title = $"{game.ShortAwayTeam} at {game.ShortHomeTeam}";
        await SendPush(title, $"Recording started");

        var outputDirectory = Path.Combine(_downloadPath, game.League);
        Directory.CreateDirectory(outputDirectory);

        var localGameTime = game.GameTime.ToLocalTime();
        var filename = $"{localGameTime:MMdd}-{game.ShortAwayTeam}-{game.ShortHomeTeam}".Replace(" ", "-");

        // Doubleheader handling
        if (Directory.EnumerateFiles(outputDirectory, $"{filename}*").Any())
            filename += $"-game2";

        for (var i = 1; i <= _maxAttempts; i++)
        {
            var swStream = new Stopwatch();
            swStream.Start();

            var outputPath = Path.Combine(outputDirectory, $"{filename}-{i:00}.ts");
            var nfoPath = Path.Combine(outputDirectory, $"{filename}-{i:00}.nfo");
            var logPath = Path.Combine(outputDirectory, $"{filename}-{i:00}.log");
            var posterPath = Path.Combine(outputDirectory, $"{filename}-{i:00}.png");

            try
            {
                var stream = await GetGameStream(game.League, game.HomeTeam);

                // use LAN domain for recording if defined, else the main domain
                var hlsDomain = !string.IsNullOrEmpty(_lazyDanLanUrl)
                    ? _lazyDanLanUrl
                    : _lazyDanUrl;

                // "hls://" prefix needed after changing up the URL scheme, assuming streamlink couldn't figure it out
                var url = $"hls://{hlsDomain}{stream.Url}";

                _logger.LogInformation("Downloading {provider} stream to {outputPath}, attempt {attempt}", stream.Provider, outputPath, i);

                var result = await Cli.Wrap("streamlink")
                    .WithArguments($"{url} best -o {outputPath} {_streamlinkArgs}")
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync();

                swStream.Stop();
                var elapsedMin = Math.Round(swStream.Elapsed.TotalMinutes);

                var exitCode = result.ExitCode;
                var stdOut = result.StandardOutput;
                var stdErr = result.StandardError;

                _logger.LogInformation("{provider} stream exited with code {exitCode} after {duration} minutes, attempt {attempt}", stream.Provider, exitCode, elapsedMin, i);

                var log = new StringBuilder();
                log.AppendLine($"Exit Code: {exitCode}");
                log.AppendLine($"Stream Provider: {stream.Provider}");
                log.AppendLine($"Stream URL: {stream.Url}");
                log.AppendLine($"Duration (min): {elapsedMin}");
                log.AppendLine($"Std Out: {stdOut}");
                log.AppendLine($"Std Err: {stdErr}");
                File.WriteAllText(logPath, log.ToString());

                if (!File.Exists(outputPath))
                {
                    Thread.Sleep(30 * 1000);
                    continue;
                }

                // some ts files have errors that lead to a ~26 hour duration, which screws up seeking
                // having ffmpeg remux the file usually fixes the duration
                var duration = await StreamUtils.GetDuration(outputPath);
                if (duration > 86400)
                {
                    _logger.LogInformation("File {outputPath} has an abnormal duration of {duration} seconds, remuxing...", outputPath, duration);

                    // ffmpeg throws error if the file extension isn't a video container format
                    var tempPath = Path.GetTempFileName() + ".ts";

                    await StreamUtils.RemuxFile(outputPath, tempPath);
                    File.Delete(outputPath);
                    File.Move(tempPath, outputPath);
                }

                var nfoXml = StreamUtils.GetNfoFile(game, i);
                nfoXml.Save(nfoPath);

                var posterBytes = _posterService.CombineLogos(game.League, game.HomeTeam, game.AwayTeam);

                if (posterBytes != null)
                    File.WriteAllBytes(posterPath, posterBytes);

                await SendWebhooks();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            var g = await _gameService.GetGame(game.Id);
            _logger.LogInformation("Game is {state}, attempt {attempt}", g.State, i);

            if (g.State == GameState.Final)
                break;
        }

        if (swGame.Elapsed.TotalHours < _gameHoursMin)
            await SendPush(title, "Recording ended early :(");

        _logger.LogInformation("Game over, recording complete");
        dvrEntry.Completed = true;
        await _gameService.UpdateDownload(dvrEntry);
    }

    public async Task SendPush(string title, string message)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.pushbullet.com/v2/pushes");
            request.Headers.Add("Access-Token", _pushbulletAccessToken);
            request.Content = new StringContent($"{{\"type\": \"note\", \"title\": \"{title}\", \"body\":\"{message}\"}}", Encoding.UTF8, "application/json");
            await _client.SendAsync(request);
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, exc.Message);
        }
    }

    public async Task SendWebhooks()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_jellyfinUrl}/Library/Refresh");
            request.Headers.Add("X-Emby-Token", _jellyfinAccessToken);
            await _client.SendAsync(request);

        }
        catch (Exception exc)
        {
            _logger.LogError(exc, exc.Message);
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_plexUrl}/library/sections/all/refresh");
            request.Headers.Add("X-Plex-Token", _plexAccessToken);
            await _client.SendAsync(request);
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, exc.Message);
        }
    }

    private async Task<string> GetStreamFromProvider(IGameStreamProvider provider, string league, string team)
    {
        if (league == League.Mlb)
            return await provider.GetMlbStream(team);
        else if (league == League.Nba)
            return await provider.GetNbaStream(team);
        else if (league == League.Wnba)
            return await provider.GetWnbaStream(team);
        else if (league == League.Nfl)
            return await provider.GetNflStream(team);
        else if (league == League.Nhl)
            return await provider.GetNhlStream(team);
        else if (league == League.Cfb)
            return await provider.GetCfbStream(team);
        else
            throw new Exception("Unknown league");
    }

    private async Task SanityTestStream(string spoofUrl)
    {
        var response = await _client.GetAsync($"{_lazyDanUrl}{spoofUrl}");
        response.EnsureSuccessStatusCode();
    }
}