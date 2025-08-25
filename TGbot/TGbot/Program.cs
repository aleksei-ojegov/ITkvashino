using static Telegram.Bot.TelegramBotClient;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using TGbot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System;

class Program
{
    private static ITelegramBotClient _botClient;

    private static ReceiverOptions _receiverOptions;

    private static UpdateHandler _updateHandler;

    private static readonly List<string> _drugs = new List<string>
    {
        "Парацетамол — жаропонижающее",
        "Ибупрофен — противовоспалительное",
        "Амоксициллин — антибиотик"
    };

    static async Task Main()
    {
        _updateHandler = new UpdateHandler();
        _botClient = new TelegramBotClient("8214585324:AAE0bJuq5L_2ASM3dfKiOZNKomZYN5AtMBs");
        _receiverOptions = new ReceiverOptions 
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            },

        };

        using var cts = new CancellationTokenSource();

        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token); // Запускаем бота

        var me = await _botClient.GetMe();
        Console.WriteLine($"{me.FirstName} запущен!");

        await Task.Delay(-1);
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    {

                        var message = update.Message;

                        var user = message.From;
                        var chat = message.Chat;
                        if (message.Text == "/start")
                        {
                            var replyKeyboard = new ReplyKeyboardMarkup(
                                    new List<KeyboardButton[]>()
                                    {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Просмотреть свои лекарства"),
                                            new KeyboardButton("Добавить лекарства"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Просмотреть оставшиеся лекарства на сегодня")
                                        },
                                    })
                            {

                                ResizeKeyboard = true,
                            };

                            await botClient.SendMessage(
                                chat.Id,
                                "Жду команды",
                                replyMarkup: replyKeyboard);

                            return;

                        }
                        if (message.Text == "Просмотреть свои лекарства")
                        {
                            // Отправляем первое лекарство
                            await SendDrug(botClient, message.Chat.Id, 0);
                        }

                        return;
                    }

                case UpdateType.CallbackQuery:
                    {
                        var callback = update.CallbackQuery!;
                        var data = callback.Data!.Split(':');

                        if (data[0] == "drug")
                        {
                            int index = int.Parse(data[1]);

                            // редактируем то же сообщение
                            await EditDrug(botClient, callback.Message!.Chat.Id, callback.Message.MessageId, index);

                            // убираем "часики"
                            await botClient.AnswerCallbackQuery(callback.Id);
                        }
                        return;
                    }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static async Task SendDrug(ITelegramBotClient botClient, long chatId, int index)
    {
        var text = _drugs[index];
        var keyboard = GetKeyboard(index);

        await botClient.SendMessage(chatId, text, replyMarkup: keyboard);
    }

    private static async Task EditDrug(ITelegramBotClient botClient, long chatId, int messageId, int index)
    {
        var text = _drugs[index];
        var keyboard = GetKeyboard(index);

        await botClient.EditMessageText(chatId, messageId, text, replyMarkup: keyboard);
    }

    private static InlineKeyboardMarkup GetKeyboard(int index)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        var row = new List<InlineKeyboardButton>();

        if (index > 0)
            row.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"drug:{index - 1}"));

        if (index < _drugs.Count - 1)
            row.Add(InlineKeyboardButton.WithCallbackData("➡️ Далее", $"drug:{index + 1}"));

        if (row.Count > 0)
            buttons.Add(row.ToArray());

        return new InlineKeyboardMarkup(buttons);
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        // Тут создадим переменную, в которую поместим код ошибки и её сообщение 
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}