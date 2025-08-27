using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITkvashino.Core;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TGbot
{
        public class UpdateHandler
        {
            private readonly DrugDealer _drugDealer;
            private List<Drug> _drugs;

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
                    await _drugDealer.SendDrug(botClient, message.Chat.Id, 0, _drugs, "my");
                }
                else if (message.Text == "Просмотреть все лекарства")
                {
                await _drugDealer.SendDrug(botClient, message.Chat.Id, 0, _drugs, "all");
                }
            }

            private async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
            {
                var data = callbackQuery.Data.Split(':');

                if (data[0] == "drug")
                {
                    int index = int.Parse(data[1]);
                    string mode = data[2];

                    await _drugDealer.EditDrug(botClient, callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, index, _drugs, mode);

                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                }
                if (data[0] == "add")
                {
                int index = int.Parse(data[1]);
                string mode = data[2];
                var findedDrug = _drugs.FirstOrDefault(d => d.Name == _drugs[index].Name);

                await botClient.DeleteMessage(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId);

                await botClient.SendMessage(
                    callbackQuery.Message!.Chat.Id,
                    $"Лекарство {findedDrug.Name} добавлено");

                await _drugDealer.SendDrug(botClient, callbackQuery.Message!.Chat.Id, index, _drugs, mode);

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
