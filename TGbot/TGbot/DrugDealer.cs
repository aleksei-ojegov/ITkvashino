using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITkvashino.Core;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace TGbot
{
    public class DrugDealer
    {
        public async Task SendDrug(ITelegramBotClient botClient, long chatId, int index, List<Drug> drugs, string mode)
        {
            string text = GetDrugText(drugs[index], mode);
            var keyboard = GetKeyboard(index, mode, drugs);

            await botClient.SendMessage(chatId, text, replyMarkup: keyboard);
        }

        public async Task EditDrug(ITelegramBotClient botClient, long chatId, int messageId, int index, List<Drug> drugs, string mode)
        {
            string text = GetDrugText(drugs[index], mode);
            var keyboard = GetKeyboard(index, mode, drugs);

            await botClient.EditMessageText(chatId, messageId, text, replyMarkup: keyboard);
        }

        private static InlineKeyboardMarkup GetKeyboard(int index, string mode, List<Drug> drugs)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            var row = new List<InlineKeyboardButton>();

            if (index > 0)
                row.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"drug:{index - 1}:{mode}"));

            if (index < drugs.Count - 1)
                row.Add(InlineKeyboardButton.WithCallbackData("➡️ Далее", $"drug:{index + 1}:{mode}"));

            if (row.Count > 0)
                buttons.Add(row.ToArray());

            return new InlineKeyboardMarkup(buttons);
        }

        private static string GetDrugText(Drug drug, string mode)
        {
            if (mode == "all")
            {
                return $"💊 Наименование: {drug.Name}\n\n" +
                       $"📝 Описание: {drug.Description}\n\n" +
                       $"📅 Срок годности: {drug.ShelfLife}\n" +
                       $"💊 Таблеток в упаковке: {drug.TabletsInPack}\n" +
                       $"🏥 Показания: {drug.Indications}\n" +
                       $"📋 Группа: {drug.Group}";
            }
            else if (mode == "my")
            {
                return $"💊 Наименование: {drug.Name}\n\n" +
                       $"📝 Описание: {drug.Description}\n\n" +
                       $"📅 Срок годности: {drug.ShelfLife}\n" +
                       $"💊 Таблеток в упаковке: {drug.TabletsInPack}\n" +
                       $"⏰ Частота приёма: {drug.Dosage.Description}\n" +
                       $"📅 Дата покупки: {DateTime.Now}\n\n" +
                       $"🏥 Показания: {drug.Indications}\n" +
                       $"📋 Группа: {drug.Group}";
            }
            else
            {
                return "Неверная команда";
            }
        }
    }
}
