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
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using ClientLibrary;

class Program
{
    private static ITelegramBotClient _botClient;

    private static ReceiverOptions _receiverOptions;

    private static UpdateHandler _updateHandler;

    private static DatabaseService _db;

    private static DrugDataLoader _drugDataLoader = new DrugDataLoader();

    private static string _filePath = "drugs_test.txt";

    private static List<Drug> Drugs = new List<Drug> { };

    private static DrugDealer _drugDealer = new DrugDealer();
    static async Task Main()
    {
        Drugs = _drugDataLoader.LoadDrugsFromFile(_filePath);
        //_db = new DatabaseService("Server=localhost;Port=3306;Database=Drug;User ID=root;Password=;SslMode=None;");
        //Drugs = await _db.GetAllDrugsAsync();
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

        //==============Пример работы с методами===================================================
        var methods = new Methods();
        var drugs = await methods.GetAllDrugs();

        Console.WriteLine("\n=== Список лекарств получен в Program.cs ===");
        foreach (var d in drugs)
        {
          Console.WriteLine($"{d.Id}: {d.Name} ({d.Dosage}), упаковка {d.TabletsInPack} табл.");
        }
        //=========================================================================================

        await Task.Delay(-1);
    }
}