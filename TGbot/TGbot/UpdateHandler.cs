using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ClientLibrary;
using System.Globalization;
using Org.BouncyCastle.Bcpg;

namespace TGbot
{
  public class UpdateHandler
  {
    private readonly DrugDealer _drugDealer;
    private List<Drug> _drugs;
    private List<PersonalDrug> _userDrugs;
    private Methods Methods = new Methods();
    private static Dictionary<long, Drug> _awaitingCustomTimes = new();

    public UpdateHandler(DrugDealer drugDealer, List<Drug> drugs)
    {
      _drugDealer = drugDealer;
      _drugs = drugs;
    }
    public UpdateHandler()
    {

    }

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
      _userDrugs = await Methods.GetAllPersonalDrugs(user.Id, _drugs);

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
              new KeyboardButton("Добавить лекарства"),
            },
            new KeyboardButton[]
            {
              new KeyboardButton("Просмотреть все лекарства")
            },
          })
        {
          ResizeKeyboard = true,
        };

        await botClient.SendMessage(
          chat.Id,
          "Жду команды",
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
        }

    private async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
      var data = callbackQuery.Data.Split(':');

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
        var user = callbackQuery.From;
        var userId = user.Id;
        int index = int.Parse(data[1]);
        string mode = data[2];
        var findedDrug = _drugs.FirstOrDefault(d => d.Name == _drugs[index].Name);
        Methods.AddPersonalDrugs(userId, findedDrug);
        _userDrugs = await Methods.GetAllPersonalDrugs(userId, _drugs);
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

                // Сохраняем Id лекарства, чтобы знать, для какого указывать время
               _awaitingCustomTimes[callbackQuery.Message.Chat.Id] = drug;

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


                await botClient.SendMessage(
                    callbackQuery.Message.Chat.Id,
                    $"✅ Напоминания для {drug.Name} установлены: {string.Join(", ", reminderTimes.Select(t => t.ToString(@"hh\:mm")))}");
            }

            if (data[0] == "reminderCustom")
            {
                int index = int.Parse(data[1]);
                string mode = data[2];
                var drug = _userDrugs[index];

                _awaitingCustomTimes[callbackQuery.Message.Chat.Id] = drug;

                await botClient.SendMessage(
                    callbackQuery.Message.Chat.Id,
                    $"Введите время для приёма лекарства {drug.Name}.\nФормат: `HH:mm, HH:mm, ...`",
                    parseMode: ParseMode.Markdown);
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
  }
}
