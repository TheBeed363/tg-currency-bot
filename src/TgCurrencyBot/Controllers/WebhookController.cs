using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using TgCurrencyBot.Services;

namespace TgCurrencyBot.Controllers;

public class WebhookController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Post([FromServices] HandleUpdateService handleUpdateService, [FromBody] Update update)
    {
        await handleUpdateService.EchoAsync(update);

        return Ok();
    }
}