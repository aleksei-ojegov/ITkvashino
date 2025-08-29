using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using ClientLibrary;
using static System.Net.Mime.MediaTypeNames;
using Telegram.Bot.Types;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;

namespace TGbot
{
  /// <summary>
  /// Класс для обработки объекто в "Карусель"
  /// </summary>
  public class DrugDealer
  {
    int MessegePhoto;

    #region Методы
    /// <summary>
    /// Отправка первого лекарства из списка в "Карусель"
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="botClient">Клиент</param>
    /// <param name="chatId">Id чата</param>
    /// <param name="index">Порядковый номер лекарства</param>
    /// <param name="drugs">Список лекарств</param>
    /// <param name="mode">Режим просмотра</param>
    /// <returns></returns>
    public async Task SendDrug<T>(ITelegramBotClient botClient, long chatId, int index, List<T> drugs, string mode) where T : Drug
    {
      string text = GetDrugText(drugs[index], mode);
      var keyboard = GetKeyboard(index, mode, drugs);

      await botClient.SendMessage(chatId, text, replyMarkup: keyboard);
    }

    /// <summary>
    /// Редактирование "Карусели"
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="botClient">Клиент</param>
    /// <param name="chatId">Id чата</param>
    /// <param name="messageId">Id сообщения</param>
    /// <param name="index">Порядковый номер лекарства</param>
    /// <param name="drugs">Список лекарств</param>
    /// <param name="mode">Режим просмотра</param>
    /// <returns></returns>
    public async Task EditDrug<T>(ITelegramBotClient botClient, long chatId, int messageId, int index, List<T> drugs, string mode) where T: Drug
    {
      string text = GetDrugText(drugs[index], mode);
      var keyboard = GetKeyboard(index, mode, drugs);

      await botClient.EditMessageText(chatId, messageId, text, replyMarkup: keyboard);
    }

    async Task DeleteMessageAsync(long chatId, int messageId, string botToken)
    {
      using var client = new HttpClient();
      var url = $"https://api.telegram.org/bot{botToken}/deleteMessage";

      var payload = new Dictionary<string, string>
    {
        { "chat_id", chatId.ToString() },
        { "message_id", messageId.ToString() }
    };

      var response = await client.PostAsync(url, new FormUrlEncodedContent(payload));
      response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Генерация клавиатуры
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="index">Порядковый номер лекарства</param>
    /// <param name="mode">Режим просмотра</param>
    /// <param name="drugs">Список лекарств</param>
    /// <returns></returns>
    private static InlineKeyboardMarkup GetKeyboard<T>(int index, string mode, List<T> drugs) where T : Drug
    {
      var buttons = new List<InlineKeyboardButton[]>();
      var navigationRow = new List<InlineKeyboardButton>();

      if (index > 0)
      {
        navigationRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"drug:{index - 1}:{mode}"));
      }

      if (index == 0)
      {
        navigationRow.Add(InlineKeyboardButton.WithCallbackData("⏭️ В конец", $"drug:{drugs.Count - 1}:{mode}"));
      }

      if (index == drugs.Count - 1)
      {
        navigationRow.Add(InlineKeyboardButton.WithCallbackData("⏮️ В начало", $"drug:{0}:{mode}"));
      }

      if (index < drugs.Count - 1)
      {
        navigationRow.Add(InlineKeyboardButton.WithCallbackData("➡️ Далее", $"drug:{index + 1}:{mode}"));
      }

      if (navigationRow.Count > 0)
      {
        buttons.Add(navigationRow.ToArray());
      }

      if(mode == "all")
      {
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("✅ Добавить лекарство к себе", $"add:{index}:{mode}") });
      }

      if ((mode == "my") && (drugs[index] is PersonalDrug drug))
      {
        if(drug.IsActive == false)
        {
          buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("✅ Начать лечение", $"start:{index}:{mode}") });
        }
        if (drug.IsActive == true)
        {
          buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⛔️ Закончить лечение", $"end:{index}:{mode}") });
        }
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("💊 Редактировать количество таблеток", $"changeQuantity:{index}:{mode}") });
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ Удалить лекарство", $"delete:{index} : {mode}")});
      }

      return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Формирование текста для "Карусели"
    /// </summary>
    /// <param name="drug">Список лекарств</param>
    /// <param name="mode">Режим просмотра</param>
    /// <returns></returns>
    private static string GetDrugText(Drug drug, string mode)
    {
      if (mode == "all")
      {
        return $"☘️ Наименование: {drug.Name}\n\n" +
                $"📝 Описание: {drug.Description}\n\n" +
                $"🍎 Срок годности: {drug.ShelfLife}\n" +
                $"💊 Таблеток в упаковке: {drug.TabletsInPack}\n" +
                $"🏥 Показания: {drug.Indications}\n" +
                $"📋 Группа: {drug.Group}";
      }
      else if ((mode == "my") && (drug is PersonalDrug personalDrug))
      {
        return $"☘️ Наименование: {drug.Name}\n\n" +
                $"📝 Описание: {drug.Description}\n\n" +
                $"🍎 Срок годности: {drug.ShelfLife}\n" +
                $"💊 Таблеток в упаковке: {personalDrug.Tablets}\n" +
                $"⏰ Частота приёма: {drug.Dosage.Description}\n" +
                $"📅 Дата покупки: {personalDrug.PurchaseDate:dd.MM.yyyy}\n\n" +
                $"{GetExpirationWarning(drug.ShelfLife, personalDrug.PurchaseDate)}"+
                $"🏥 Показания: {drug.Indications}\n\n" +
                $"📋 Группа: {drug.Group}";
      }
      else
      {
        return "Неверная команда";
      }
    }

    /// <summary>
    /// Формирование текста о сроке годности
    /// </summary>
    /// <param name="shelfLifeText">Срок годности</param>
    /// <param name="purchaseDate">Дата покупки</param>
    /// <returns></returns>
    private static string GetExpirationWarning(string shelfLifeText, DateTime purchaseDate)
    {
      if (TryParseYears(shelfLifeText, out int years))
      {
        var expirationDate = purchaseDate.AddYears(years);
        var timeLeft = expirationDate - DateTime.Now;

        string warning = $"⌛️ Истекает: {expirationDate:dd.MM.yyyy}\n";

        if (timeLeft.TotalDays <= 0)
        {
          warning = "❌ ПРОСРОЧЕНО! Не используйте это лекарство!\n";
        }
        else if (timeLeft.TotalDays <= 30) // Меньше месяца
        {
          warning = $"🚨 СРОЧНО! Истекает через {(int)timeLeft.TotalDays} дней!\n";
        }
        else if (timeLeft.TotalDays <= 90) // Меньше 3 месяцев
        {
          warning = $"⚠️ Внимание! Истекает через ~{(int)(timeLeft.TotalDays / 30)} месяцев\n";
        }

        return warning + "\n";
      }

      return string.Empty;
    }

    /// <summary>
    /// Метод парсинга лет
    /// </summary>
    /// <param name="shelfLifeText">Срок годности</param>
    /// <param name="years">Года</param>
    /// <returns></returns>
    private static bool TryParseYears(string shelfLifeText, out int years)
    {
      years = 0;

      var text = shelfLifeText.Trim().ToLower();

      var match = Regex.Match(text, @"\d+");
      if (match.Success && int.TryParse(match.Value, out years))
      {
        return true;
      }

      return false;
    }
    #endregion
  }
}
