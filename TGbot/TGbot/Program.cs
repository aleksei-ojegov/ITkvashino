using static Telegram.Bot.TelegramBotClient;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using TGbot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using ITkvashino.Core;

class Program
{
    private static ITelegramBotClient _botClient;

    private static ReceiverOptions _receiverOptions;

    private static UpdateHandler _updateHandler;

    private static DrugDataLoader _drugDataLoader = new DrugDataLoader();

    private static string _filePath = "drugs_test.txt";

    private static List<Drug> Drugs = new List<Drug> { };

    private static DrugDealer _drugDealer = new DrugDealer();
    static async Task Main()
    {
        Drugs = _drugDataLoader.LoadDrugsFromFile(_filePath);
        _updateHandler = new UpdateHandler(_drugDealer, Drugs);
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

        _botClient.StartReceiving(
            _updateHandler.HandleUpdateAsync,
            _updateHandler.HandleErrorAsync,
            _receiverOptions,
            cts.Token);

        var me = await _botClient.GetMe();
        Console.WriteLine($"{me.FirstName} запущен!");

        await Task.Delay(-1);
    }
}