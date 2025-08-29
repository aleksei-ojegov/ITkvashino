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

namespace TGbot
{
    public class DrugDealer
    {
        public async Task SendDrug<T>(ITelegramBotClient botClient, long chatId, int index, List<T> drugs, string mode) where T : Drug
        {
            string text = GetDrugText(drugs[index], mode);
            var keyboard = GetKeyboard(index, mode, drugs);

            await botClient.SendMessage(chatId, text, replyMarkup: keyboard);
        }

        public async Task EditDrug<T>(ITelegramBotClient botClient, long chatId, int messageId, int index, List<T> drugs, string mode) where T: Drug
        {
            string text = GetDrugText(drugs[index], mode);
            var keyboard = GetKeyboard(index, mode, drugs);

            await botClient.EditMessageText(chatId, messageId, text, replyMarkup: keyboard);
        }

        private static InlineKeyboardMarkup GetKeyboard<T>(int index, string mode, List<T> drugs) where T : Drug
        {
            var buttons = new List<InlineKeyboardButton[]>();

            var navigationRow = new List<InlineKeyboardButton>();
            if (index == 0)
            {
                navigationRow.Add(InlineKeyboardButton.WithCallbackData("⏭️ В конец", $"drug:{drugs.Count - 1}:{mode}"));
            }

            if (index == drugs.Count - 1)
            {
                navigationRow.Add(InlineKeyboardButton.WithCallbackData("⏮️ В начало", $"drug:{0}:{mode}"));
            }

            if (index > 0)
            {
                navigationRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"drug:{index - 1}:{mode}"));
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
                       $"💊 Таблеток в упаковке: {drug.TabletsInPack}\n" +
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

        private static string GetExpirationWarning(string shelfLifeText, DateTime purchaseDate)
        {
            // Парсим количество лет из строки
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

        private static bool TryParseYears(string shelfLifeText, out int years)
        {
            years = 0;

            // Убираем лишние пробелы и приводим к нижнему регистру
            var text = shelfLifeText.Trim().ToLower();

            // Пытаемся найти число в строке
            var match = Regex.Match(text, @"\d+");
            if (match.Success && int.TryParse(match.Value, out years))
            {
                return true;
            }

            return false;
        }
    }
}
