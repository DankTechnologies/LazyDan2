using LazyDan2.Services;
using Microsoft.AspNetCore.Mvc;

namespace LazyDan2.Controllers;

[ApiController]
[Route("[controller]")]
public class ChannelController : ControllerBase
{
    private readonly StreamService _streamService;
    private readonly string _lazyDanUrl;
    public ChannelController(StreamService streamService, IConfiguration configuration)
    {
        _streamService = streamService;
        _lazyDanUrl = configuration["LazyDanUrl"];
    }

    [HttpGet]
    [Route("{channel}")]
    public async Task<IActionResult> GetStream(string channel)
    {
        var stream = await _streamService.GetGameStream(channel);
        return Redirect(_lazyDanUrl + stream.Url);
    }
}
