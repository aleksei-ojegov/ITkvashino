using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary
{
    internal class RemindTimer
    {
        private static async Task StartReminderLoop(ITelegramBotClient botClient)
        {
            while (true)
            {
                var now = DateTime.Now;

                // Берём список всех активных лекарств из базы
                var activeDrugs = PersonalDrugRepository.GetActiveDrugs();

                foreach (var drug in activeDrugs)
                {
                    if (drug.Times == null) continue;

                    foreach (var time in drug.Times)
                    {
                        // Сравниваем текущее время с временем приёма
                        if (time.Hours == now.Hour && time.Minutes == now.Minute)
                        {
                            await botClient.SendTextMessageAsync(
                                chatId: drug.TelegramUserId, // или ChatId, если у тебя так хранится
                                text: $"💊 Пора принять {drug.Name}!\n" +
                                      $"Дозировка: {drug.Dosage}"
                            );
                        }
                    }
                }

                // проверяем каждую минуту
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
    }
}
