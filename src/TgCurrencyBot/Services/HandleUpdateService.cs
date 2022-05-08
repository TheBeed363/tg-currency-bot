using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TgCurrencyBot.Models;
using System.Globalization;

namespace TgCurrencyBot.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;

    public HandleUpdateService(ILogger<HandleUpdateService> logger, ITelegramBotClient botClient, IMemoryCache memoryCache)
    {
        _logger = logger;
        _botClient = botClient;
        _memoryCache = memoryCache;
    }

    public async Task EchoAsync(Update update)
    {
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            UpdateType.Message => BotOnMessageReceived(update.Message!),
            UpdateType.EditedMessage => BotOnMessageReceived(update.EditedMessage!),
            UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery!),
            UpdateType.InlineQuery => BotOnInlineQueryReceived(update.InlineQuery!),
            UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult!),
            _ => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (Exception e)
        {
            await HandleErrorAsync(e);
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        _logger.LogInformation("Receive message type: {messageType}", message.Type);
        _memoryCache.TryGetValue("key_currency", out Currency model);

        if (message.Type != MessageType.Text)
        {
            return;
        }

        var action = message.Text!.Split(' ')[0] switch
        {
            "/inline" => SendInlineKeyboard(_botClient, message),
            "/keyboard" => SendReplyKeyboard(_botClient, message),
            "/remove" => RemoveKeyboard(_botClient, message),
            "/photo" => SendFile(_botClient, message),
            "/request" => RequestContactAndLocation(_botClient, message),
            "/currency" => GetCurrency(_botClient, message, model, _logger),
            _ => Usage(_botClient, message)
        };

        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {sentMessageId}", sentMessage.MessageId);

        static async Task<Message> SendInlineKeyboard(ITelegramBotClient bot, Message message)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            // Simulate longer running task
            await Task.Delay(500);

            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    }
                });

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Choose", replyMarkup: inlineKeyboard);
        }

        static async Task<Message> SendReplyKeyboard(ITelegramBotClient bot, Message message)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
            new[]
            {
                new KeyboardButton[] { "1.1", "1.2" },
                new KeyboardButton[] { "2.1", "2.2" },
            })
            {
                ResizeKeyboard = true
            };

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Choose", replyMarkup: replyKeyboardMarkup);
        }

        static async Task<Message> RemoveKeyboard(ITelegramBotClient bot, Message message)
        {
            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
        }

        static async Task<Message> SendFile(ITelegramBotClient bot, Message message)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);
            const string filePath = @"Files/tux.png";
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

            return await bot.SendPhotoAsync(chatId: message.Chat.Id, photo: new InputOnlineFile(fileStream, fileName), caption: "Nice Picture");
        }

        static async Task<Message> RequestContactAndLocation(ITelegramBotClient bot, Message message)
        {
            ReplyKeyboardMarkup RequestReplyKeyboard = new(
                new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Who or Where are you?", replyMarkup: RequestReplyKeyboard);
        }

        static async Task<Message> GetCurrency(ITelegramBotClient bot, Message message, Currency model, ILogger logger)
        {
            

            try
            {
                var cultur = new CultureInfo("ru-RU", false);
                await bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                var usd = model.USD;
                var eur = model.EUR;
                var cny = model.CNY;

                // Simulate longer running task
                await Task.Delay(500);

                InlineKeyboardMarkup inlineKeyboard = new(
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("USD", $"{usd} руб."),
                        InlineKeyboardButton.WithCallbackData("EUR", $"{eur} руб."),
                        InlineKeyboardButton.WithCallbackData("UAN", $"{cny} руб."),
                    });
                return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Выберите валюту:", replyMarkup: inlineKeyboard);
            }
            catch (Exception e) {
                logger.LogError(e.Message);
                throw;
            }
        }

        static async Task<Message> Usage(ITelegramBotClient bot, Message message)
        {
            const string usage = "Команда:\n/currency - проверка курса валют";

            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: usage, replyMarkup: new ReplyKeyboardRemove());
        }
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        await _botClient.AnswerCallbackQueryAsync(callbackQueryId: callbackQuery.Id, text: $"Результат: {callbackQuery.Data}");

        await _botClient.SendTextMessageAsync(chatId: callbackQuery.Message.Chat.Id, text: $"Результат: {callbackQuery.Data}");
    }

    #region Inline Mode
    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery)
    {
        _logger.LogInformation("Received inline query from: {inlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "3",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent(
                    "hello"
                )
            )
        };

        await _botClient.AnswerInlineQueryAsync(inlineQueryId: inlineQuery.Id, results: results, isPersonal: true, cacheTime: 0);
    }

    private Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
    {
        _logger.LogInformation("Received inline result: {chosenInlineResultId}", chosenInlineResult.ResultId);

        return Task.CompletedTask;
    }
    #endregion

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {updateType}", update.Type);

        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(Exception exception)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        return Task.CompletedTask;
    }
}