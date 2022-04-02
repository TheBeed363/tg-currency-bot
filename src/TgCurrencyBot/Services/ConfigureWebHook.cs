using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TgCurrencyBot.Models;

namespace TgCurrencyBot.Services;

public class ConfigureWebHook : IHostedService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private readonly BotConfiguration _botConfig;

    public ConfigureWebHook(ILogger<ConfigureWebHook> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _services = serviceProvider;
        _botConfig = configuration.GetSection("BotConfiguration").Get<BotConfiguration>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        // Configure custom endpoint per Telegram API recommendations:
        // https://core.telegram.org/bots/api#setwebhook
        // If you'd like to make sure that the Webhook request comes from Telegram, we recommend
        // using a secret path in the URL, e.g. https://www.example.com/<token>.
        // Since nobody else knows your bot's token, you can be pretty sure it's us.
        var webHookAddress = $@"{_botConfig.HostAddress}/bot/{_botConfig.BotToken}";
        _logger.LogInformation("Setting webhook: {webHookAddress}", webHookAddress);

        await botClient.SetWebhookAsync(url: webHookAddress, allowedUpdates: Array.Empty<UpdateType>(), cancellationToken: cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        // Remove webhook upon app shutdown
        _logger.LogInformation("Removing webhook");

        await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
    }
}