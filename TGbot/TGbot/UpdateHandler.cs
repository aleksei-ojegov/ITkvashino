using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClientLibrary;
using Org.BouncyCastle.Bcpg;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Net.Http.Headers;

namespace TGbot
{
  /// <summary>
  /// Обработчик входящих обновлений от Telegram.
  /// </summary>
  public class UpdateHandler
  {
    #region Поля и Свойства

    /// <summary>
    /// Обработчик "Карусели"
    /// </summary>
    private readonly DrugDealer _drugDealer;
    /// <summary>
    /// Список лекарств из БД
    /// </summary>
    private List<Drug> _drugs;
    /// <summary>
    /// Список лекарств у пользователя
    /// </summary>
    private List<PersonalDrug> _userDrugs;
    /// <summary>
    /// Методы для общения с БД
    /// </summary>
    private Methods _methods = new Methods();
    /// <summary>
    /// Переменная для ожидания ответа с временем напоминания
    /// </summary>
    private static Dictionary<long, PersonalDrug> _awaitingCustomTimes = new();
    /// <summary>
    /// Переменная для ожидания ответа с количестов добавленных таблеток
    /// </summary>
    private static Dictionary<long, PersonalDrug> _awaitingTabletsQuantity = new();
    /// <summary>
    /// Переменная для старта запуска напоминаний
    /// </summary>
    private bool _isStartReminder = false;
    /// <summary>
    /// Переменная для ожидания ответа с наименованием лекарства
    /// </summary>
    private static long _awaitingDrugName = new();

    #endregion

    #region <IUpdateHandler>
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
      try
      {
        switch (update.Type)
        {
          case UpdateType.Message:
            await HandleMessage(botClient, update.Message);
            break;

          case UpdateType.CallbackQuery:
            await HandleCallbackQuery(botClient, update.CallbackQuery);
            break;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
    }

    private async Task HandleMessage(ITelegramBotClient botClient, Message message)
    {
      var user = message.From;
      _userDrugs = await _methods.GetAllPersonalDrugs(user.Id, _drugs);
    if (!_isStartReminder)
     {
                _isStartReminder = true;
                Task.Run(() => RemindTimer.StartReminderLoop(botClient, _userDrugs, message.Chat.Id));
     }
      Console.WriteLine($"Найдено персональных лекарств: {_userDrugs.Count}");

      foreach (var pd in _userDrugs)
      {
        Console.WriteLine($"ID записи: {pd.Id}");
        Console.WriteLine($"ID пользователя: {pd.IdUser}");
        Console.WriteLine($"ID лекарства: {pd.IdDrug}");
        Console.WriteLine($"Таблеток: {pd.Tablets}");
        Console.WriteLine($"Дата покупки: {pd.PurchaseDate}");
        Console.WriteLine($"Активно: {pd.IsActive}");
        Console.WriteLine(new string('-', 40));
      }
      var chat = message.Chat;
      if (message.Text == "/start")
      {
        var replyKeyboard = new ReplyKeyboardMarkup(
          new List<KeyboardButton[]>()
          {
            new KeyboardButton[]
            {
              new KeyboardButton("Мои лекарства"),
             new KeyboardButton("Просмотреть все лекарства"),
            },
            //new KeyboardButton[]
            //{
            //  new KeyboardButton("Просмотреть все лекарства")
            //},
          })
        {
          ResizeKeyboard = true,
        };

        await botClient.SendMessage(
          chat.Id,
          "🙌🏿 Добро пожаловать!\n\n" +
          "Я бот для твоей домашней аптечки 😉\n\n" +
          "Благодаря мне ты можешь:\n" +
          " * Контролировать срок годности\n" +
          " * Получать уведомления о приёме\n" +
          " * Найти лекарство из библиотеки\n",
          replyMarkup: replyKeyboard);
      }
      else if (message.Text == "Мои лекарства")
      {
        await _drugDealer.SendDrug(botClient, message.Chat.Id, 0, _userDrugs, "my");
      }

      else if (message.Text == "Просмотреть все лекарства")
      {
        await _drugDealer.SendDrug(botClient, message.Chat.Id, 0, _drugs, "all");
      }

      else if (message.Text == "Добавить лекарства")
      {
        await botClient.SendMessage(
          chat.Id,
          "Введите название лекарства");
        _awaitingDrugName = message.Chat.Id;

      }

      else if (_awaitingCustomTimes.ContainsKey(message.Chat.Id))
      {
        var drug = _awaitingCustomTimes[message.Chat.Id];
        var input = message.Text;

        try
        {
          var times = input.Split(',')
          .Select(t => TimeSpan.Parse(t.Trim()))
          .ToList();

          if (times.Count != drug.Dosage.TimesPerDay)
          {
            await botClient.SendMessage(
              message.Chat.Id,
              $"⚠️ Нужно указать ровно {drug.Dosage.TimesPerDay} времени для приёма!");
            return;
          }
          drug.RemindTimes = times;
          await botClient.SendMessage(
          message.Chat.Id,
          $"✅ Напоминания для {drug.Name} установлены: {string.Join(", ", times.Select(t => t.ToString(@"hh\:mm")))}");

         _awaitingCustomTimes.Remove(message.Chat.Id);
        }

        catch
        {
          await botClient.SendMessage(
            message.Chat.Id,
            "⚠️ Неверный формат. Попробуйте снова (например: 09:00, 14:00, 20:00)");
        }
      }

      else if (_awaitingDrugName == message.Chat.Id)
      {
        var input = message.Text;
        var findedDrug = _drugs.FirstOrDefault(d => d.Name == input);
        if (findedDrug != null)
        {
          await _methods.AddPersonalDrugs(user.Id, findedDrug);
          _userDrugs = await _methods.GetAllPersonalDrugs(user.Id, _drugs);
          await botClient.SendMessage(
          message.Chat.Id,
          $"Лекарство {findedDrug.Name} добавлено");
        }
        else
        {
          await botClient.SendMessage(
          message.Chat.Id,
          $"Лекарство {input} не найдено");
        }
        _awaitingDrugName = 0;
      }

      else if (_awaitingTabletsQuantity.ContainsKey(message.Chat.Id))
      {
        var drug = _awaitingTabletsQuantity[message.Chat.Id];
        var input = message.Text;

        try
         {
           int tabletQuantity = int.Parse(input);
           var testDrug = _userDrugs.FirstOrDefault(d => d.Id == drug.Id);
           int index = Array.IndexOf(_userDrugs.ToArray(), testDrug);
           string mode = "my";

                    if (tabletQuantity >= 0)
           {
             drug.Tablets = drug.Tablets + tabletQuantity;
             await botClient.SendMessage(
             message.Chat.Id,
             $"{tabletQuantity} таблетки(ок) добавлено к лекарству {drug.Name}");
           }

           if (tabletQuantity < 0)
           {
             drug.Tablets = drug.Tablets + tabletQuantity;
             await botClient.SendMessage(
             message.Chat.Id,
             $"{tabletQuantity} таблетки(ок) убрано из лекарства {drug.Name}");
           }
           if ((tabletQuantity < 0)&&(drug.Tablets + tabletQuantity <= 0))
           {
             var keyboard = new InlineKeyboardMarkup(new[]
             {
                new []
                {
                 InlineKeyboardButton.WithCallbackData("❌ Удалить лекарство", $"delete:{index} : {mode}")
                }
             });
             await botClient.SendMessage(
               message.Chat.Id,
               $"⚠️В пачке лекарства {drug.Name} осталось 0 таблеток!!!",
               replyMarkup: keyboard);
             drug.Tablets = 0;
           }

           await _methods.UpdatePersonalDrugsTablets(drug.Id, drug.Tablets);
           _userDrugs = await _methods.GetAllPersonalDrugs(user.Id, _drugs);
           _awaitingTabletsQuantity.Remove(message.Chat.Id);
        }

        catch
        {
          await botClient.SendMessage(
            message.Chat.Id,
            "⚠️ Неверный формат. Введите целое число");
        }
      }
                
    }

    private async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
      var data = callbackQuery.Data.Split(':');
      var user = callbackQuery.From;
      var userId = user.Id;

            if (data[0] == "drug")
      {
        int index = int.Parse(data[1]);
        string mode = data[2];
        if(mode == "all")
        {
         await _drugDealer.EditDrug(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, index, _drugs, mode);
        }

        if (mode == "my")
        {
         await _drugDealer.EditDrug(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, index, _userDrugs, mode);
        }

                await botClient.AnswerCallbackQuery(callbackQuery.Id);
      }
      if (data[0] == "add")
      {
        int index = int.Parse(data[1]);
        string mode = data[2];
        var findedDrug = _drugs.FirstOrDefault(d => d.Name == _drugs[index].Name);
        await _methods.AddPersonalDrugs(userId, findedDrug);
        _userDrugs = await _methods.GetAllPersonalDrugs(userId, _drugs);
        await botClient.DeleteMessage(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId);

        await botClient.SendMessage(
            callbackQuery.Message!.Chat.Id,
            $"Лекарство {findedDrug.Name} добавлено");

        await _drugDealer.SendDrug(botClient, callbackQuery.Message!.Chat.Id, index, _drugs, mode);
      }
      if (data[0] == "start")
      {
       int index = int.Parse(data[1]);
       string mode = data[2];
       var drug = _userDrugs[index];

       var keyboard = new InlineKeyboardMarkup(new[]
       {
        new []
        {
            InlineKeyboardButton.WithCallbackData("📅 Стандарт", $"reminderStandard:{index}:{mode}"),
            InlineKeyboardButton.WithCallbackData("✍️ Сам решууууу!!!", $"reminderCustom:{index}:{mode}")
        }
        });

        await botClient.SendMessage(
           callbackQuery.Message.Chat.Id,
           $"Для лекарства {drug.Name} ({drug.Dosage.TimesPerDay} раз в день) выберите способ задания времени напоминаний:",
           replyMarkup: keyboard);
      }

      if (data[0] == "reminderStandard")
      {
        int index = int.Parse(data[1]);
        string mode = data[2];
        var drug = _userDrugs[index];

        int times = drug.Dosage.TimesPerDay;
        List<TimeSpan> reminderTimes = new();

        int startHour = 8;
        int interval = 14 / times;

        for (int i = 0; i < times; i++)
        {
          reminderTimes.Add(TimeSpan.FromHours(startHour + i * interval));
        }
        drug.IsActive = true;
        drug.RemindTimes = reminderTimes;
        await _methods.UpdatePersonalDrugActive(drug.Id, drug.IsActive);
        _userDrugs = await _methods.GetAllPersonalDrugs(user.Id, _drugs);
        await botClient.SendMessage(
          callbackQuery.Message.Chat.Id,
          $"✅ Напоминания для {drug.Name} установлены: {string.Join(", ", reminderTimes.Select(t => t.ToString(@"hh\:mm")))}");
      }

      if (data[0] == "reminderCustom")
      {
        int index = int.Parse(data[1]);
        string mode = data[2];
        var drug = _userDrugs[index];
        drug.IsActive = true;
        await _methods.UpdatePersonalDrugActive(drug.Id, drug.IsActive);
        _userDrugs = await _methods.GetAllPersonalDrugs(user.Id, _drugs);

        _awaitingCustomTimes[callbackQuery.Message.Chat.Id] = drug;

        await botClient.SendMessage(
          callbackQuery.Message.Chat.Id,
          $"Введите время для приёма лекарства {drug.Name}.\nФормат: `HH:mm, HH:mm, ...`",
          parseMode: ParseMode.Markdown);
      }

      if(data[0] == "changeQuantity")
      {
        int index = int.Parse(data[1]);
        string mode = data[2];
        var drug = _userDrugs[index];

        _awaitingTabletsQuantity[callbackQuery.Message.Chat.Id] = drug;

        await botClient.SendMessage(
           callbackQuery.Message.Chat.Id,
           $"Введите количество таблеток,которое хотите добавить\n"+
           $"Если хотите уменьшить количество таблеток,то введите число со знаком '-'(например -4)",
           parseMode: ParseMode.Markdown);
      }

      if( data[0] == "delete")
      {
        int index = int.Parse(data[1]);
        string mode = data[2];
        var drug = _userDrugs[index];

        await _methods.DeletePersonalDrug(drug.Id);
        _userDrugs = await _methods.GetAllPersonalDrugs(userId, _drugs);

        await botClient.DeleteMessage(
            callbackQuery.Message!.Chat.Id, 
            callbackQuery.Message.MessageId);

        await botClient.SendMessage(
           callbackQuery.Message.Chat.Id,
           $"Лекарство {drug.Name} удалено из вашей коллекции",
           parseMode: ParseMode.Markdown);
      }


      if (data[0] == "end")
      {
        int index = int.Parse(data[1]);
        string mode = data[2];
        var drug = _userDrugs[index];
        drug.IsActive = false;

        await _methods.UpdatePersonalDrugActive(drug.Id, drug.IsActive);
        _userDrugs = await _methods.GetAllPersonalDrugs(user.Id, _drugs);

        await botClient.DeleteMessage(
          callbackQuery.Message!.Chat.Id,
          callbackQuery.Message.MessageId);

        await botClient.SendMessage(
           callbackQuery.Message.Chat.Id,
           $"Лечение лекарством {drug.Name} закончено");
      }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
      var errorMessage = error switch
      {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => error.ToString()
      };

      Console.WriteLine(errorMessage);
      return Task.CompletedTask;
    }

    #endregion

    #region Конструкторы

    public UpdateHandler(DrugDealer drugDealer, List<Drug> drugs)
    {
     _drugDealer = drugDealer;
     _drugs = drugs;
    }
    public UpdateHandler()
    {

    }

    #endregion
  }
}
