using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary
{
  public class Methods
  {
    /// <summary>
    /// Запрос на получение всех лекарств из БД.
    /// </summary>
    /// <returns>Возращает коллекцию лекарств, если их нет то пустую коллекцию.</returns>
    public async Task<List<Drug>> GetAllDrugs()
    {
      string url = "http://localhost:5000/api/?action=drugs";

      try
      {
        var xml = await SendURLandParseXml.GetStringAsync(url);
        var drugs = SendURLandParseXml.ParseDrugs(xml);

        Console.WriteLine($"Получено записей: {drugs.Count}");
        foreach (var d in drugs)
        {
          Console.WriteLine(
              $"[{d.Id}] {d.Name} | {d.Dosage} | Табл: {d.TabletsInPack} | Срок: {d.ShelfLife}\n" +
              $"Группа: {d.Group}\n" +
              $"Показания: {d.Indications}\n" +
              $"Описание: {d.Description}\n");
        }

        return drugs;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка клиента: {ex.Message}");
        return new List<Drug>();
      }
    }

    /// <summary>
    /// Запрос на получение всех пользователей.
    /// </summary>
    /// <returns>Возращает коллекцию пользователей, если их нет то пустую коллекцию.</returns>
    public async Task<List<User>> GetAllUsers()
    {
      string url = "http://localhost:5000/api/?action=users";

      try
      {
        var xml = await SendURLandParseXml.GetStringAsync(url);
        var users = SendURLandParseXml.ParseUsers(xml);

        Console.WriteLine($"Получено пользователей: {users.Count}");
        foreach (var u in users)
        {
          Console.WriteLine(
              $"[{u.Id}] {u.Name} | ChatId: {u.ChatId}");
        }

        return users;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка клиента: {ex.Message}");
        return new List<User>();
      }
    }
  }
}
