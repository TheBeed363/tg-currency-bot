using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TgCurrencyBot.Models;
using System.Threading.Tasks;
using Telegram.Bot.Types;

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
            return Ok(model);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }

    [HttpPost, Route("api/message/update")]
    public async Task<IActionResult> Update([FromBody]Update update)
    {
        try
        {
            var commands = Bot.Commands;
            var message = update.Message;
            var botClient = await Bot.GetBotClientAsync();

            foreach (var command in commands)
            {
                if (command.Contains(message))
                {
                    await command.Execute(message, botClient);
                    break;
                }
            }

            return Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }
}