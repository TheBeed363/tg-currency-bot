using Telegram.Bot;
using TgCurrencyBot.Models.Commands;

namespace TgCurrencyBot.Models;

public class Bot
{
    private static TelegramBotClient botClient;
    private static List<Command> commandList;
    public static IReadOnlyList<Command> Commands => commandList.AsReadOnly();

    public static async Task<TelegramBotClient> GetBotClientAsync()
    {
        if (botClient != null)
        {
            return botClient;
        }

        commandList = new List<Command>();
        commandList.Add(new StartCommand());
        //TODO: Add more commands
        botClient = new TelegramBotClient(BotSettings.Key);
        var hook = string.Format(BotSettings.Url, "api/message/update");

        await botClient.SetWebhookAsync(hook);

        return botClient;
    }
}