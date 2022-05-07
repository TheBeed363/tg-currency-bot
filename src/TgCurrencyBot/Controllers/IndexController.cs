using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using TgCurrencyBot.Models;

namespace TgCurrencyBot.Controllers;

[Route("/")]
public class IndexController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;

    public IndexController(ILogger<IndexController> logger, IMemoryCache memoryCache)
    {
        _logger = logger;
        _memoryCache = memoryCache;
    }

    [HttpGet, Route("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            _memoryCache.TryGetValue("key_currency", out Currency model);
            var cultur = new CultureInfo("ru-RU", false);

            var kzt = model.KZT;
            var kzt1 = float.Parse(model.KZT, cultur.NumberFormat)/100;
            kzt = kzt1.ToString();
            model.KZT = kzt;
            
            return Ok(model);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }
}