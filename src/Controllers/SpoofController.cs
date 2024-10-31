using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace LazyDan2.Controllers;

[ApiController]
[Route("[controller]")]
public class SpoofController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpoofController> _logger;

    public SpoofController(HttpClient httpClient, ILogger<SpoofController> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    [HttpGet]
    [Route("playlist")]
    public async Task<IActionResult> Playlist(string url, string origin)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(origin))
            return BadRequest("URL and Origin cannot be null or empty");

        try
        {
            var baseUrl = Regex.Replace(url, @"\/[^/]*\.(m3u8|css)(\?.*)?$", "");

            if (!baseUrl.EndsWith('/'))
                baseUrl += '/';

            var originalUri = new Uri(url);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Referer", $"{origin}/");
            request.Headers.Add("Origin", origin);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, response.ReasonPhrase);

            var contents = await response.Content.ReadAsStringAsync();

            // if the request was redirected, use response Location as the new base URL
            if (request.RequestUri != originalUri)
            {
                baseUrl = Regex.Replace(request.RequestUri.AbsoluteUri, @"\/[^/]*\.(m3u8|css)(\?.*)?$", "");

                if (!baseUrl.EndsWith('/'))
                    baseUrl += '/';
            }

            // TS spoofing
            if (contents.Contains(".m3u8") || contents.Contains(".css"))
            {
                contents = Regex.Replace(contents, @"(^.+(m3u8|css)(\?.*)?$)", $"/spoof/playlist?url={baseUrl}$1&origin={origin}", RegexOptions.Multiline);
            }
            else if (contents.Contains("https://"))
            {
                contents = Regex.Replace(contents, @"^https://.*", $"/spoof/ts?url=$0&origin={origin}", RegexOptions.Multiline);
            }
            else
            {
                // relative path to segment
                contents = Regex.Replace(contents, @"^[^#].*", $"/spoof/ts?url={baseUrl}$0&origin={origin}", RegexOptions.Multiline);
            }

            // KEY spoofing
            if (contents.Contains("EXT-X-KEY"))
            {
                contents = Regex.Replace(contents, "URI=\"([^\"]+)", $"URI=\"/spoof/key?url=https://{request.RequestUri.Host}$1&origin={origin}");
            }

            return Ok(contents);
        }
        catch (TaskCanceledException)
        {
            return StatusCode(408, "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error occurred while fetching playlist, origin: {origin}", origin);
            return StatusCode(500, "Error occurred");
        }
    }

    [HttpGet]
    [Route("ts")]
    public async Task<IActionResult> Ts(string url, string origin)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Referer", $"{origin}/");
            request.Headers.Add("Origin", origin);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, response.ReasonPhrase);

            var stream = await response.Content.ReadAsStreamAsync();
            return File(stream, "video/MP2T");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(408, "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error occurred while fetching playlist, origin: {origin}", origin);
            return StatusCode(500, "Error occurred");
        }
    }

    [HttpGet]
    [Route("key")]
    public async Task<IActionResult> Key(string url, string origin)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Referer", $"{origin}/");
            request.Headers.Add("Origin", origin);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, response.ReasonPhrase);

            var contents = await response.Content.ReadAsStringAsync();

            return Ok(contents);
        }
        catch (TaskCanceledException)
        {
            return StatusCode(408, "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error occurred while fetching playlist, origin: {origin}", origin);
            return StatusCode(500, "Error occurred");
        }
    }
}
