using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace ClientLibrary
{
    /// <summary>
    /// Класс отправки напоминаний
    /// </summary>
    internal class RemindTimer
    {
        #region

        /// <summary>
        /// Создание клиента для работы с Телеграм ботом.
        /// </summary>
        private ITelegramBotClient _botClient;

        /// <summary>
        /// Методы общения с БД
        /// </summary>
        private static Methods _methods = new Methods();

        #endregion

        #region Методы

        /// <summary>
        /// Запуск отправки сообщений с уведомоениями
        /// </summary>
        /// <param name="botClient">Клиент</param>
        /// <param name="personalDrugs">Список лекарств пользователя</param>
        /// <param name="chatId">ID чата</param>
        /// <returns></returns>
        public static async Task StartReminderLoop(ITelegramBotClient botClient, List<PersonalDrug> personalDrugs, long chatId)
        {
            while (true)
            {
                var now = DateTime.Now;

                var activeDrugs = personalDrugs.Where(d => d.IsActive = true);

                foreach (var drug in activeDrugs)
                {
                    if (drug.RemindTimes == null) continue;

                    foreach (var time in drug.RemindTimes)
                    {
                        if (time.Hours == now.Hour && time.Minutes == now.Minute)
                        {
                            await botClient.SendMessage(
                                chatId: chatId,
                                text: $"💊 Пора принять {drug.Name}!\n" +
                                      $"Дозировка: {drug.Dosage.TabletsPerIntake}"
                            );
                            drug.Tablets--;
                            await _methods.UpdatePersonalDrugsTablets(drug.Id, drug.Tablets);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        #endregion
    }
}
