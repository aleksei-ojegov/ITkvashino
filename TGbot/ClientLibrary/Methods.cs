using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClientLibrary.ServerClass;

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

        return users;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка клиента: {ex.Message}");
        return new List<User>();
      }
    }

    /// <summary>
    /// Запрос на получение всех пользовательских лекарств.
    /// </summary>
    /// <returns>Возращает коллекцию лекарств пользователей, если их нет то пустую коллекцию.</returns>
    public async Task<List<PersonalDrug>> GetAllPersonalDrugs(long userId, List<Drug> drugs)
    {
      string url = $"http://localhost:5000/api/?action=personal&id_user={userId}";
      try
      {
        var xml = await SendURLandParseXml.GetStringAsync(url);
        var personalDrugs = SendURLandParseXml.ParsePersonalDrugs(xml);

        foreach (var pd in personalDrugs)
        {
          var fullDrug = drugs.FirstOrDefault(d => d.Id == pd.IdDrug);
          if (fullDrug != null)
          {
            pd.Name = fullDrug.Name;
            pd.Description = fullDrug.Description;
            pd.ShelfLife = fullDrug.ShelfLife;
            pd.TabletsInPack = fullDrug.TabletsInPack;
            pd.Dosage = fullDrug.Dosage;
            pd.Indications = fullDrug.Indications;
            pd.Group = fullDrug.Group;
          }
        }

        Console.WriteLine($"Найдено персональных лекарств: {personalDrugs.Count}");
        foreach (var pd in personalDrugs)
        {
          Console.WriteLine(
              $"[{pd.Id}] Пользователь: {pd.IdUser}\n" +
              $"Лекарство: {pd.IdDrug} ({pd.Name})\n" +
              $"Описание: {pd.Description}\n" +
              $"Срок годности: {pd.ShelfLife}\n" +
              $"Таблеток в упаковке: {pd.TabletsInPack}\n" +
              $"Дозировка: {pd.Dosage}\n" +
              $"Показания: {pd.Indications}\n" +
              $"Группа: {pd.Group}\n" +
              $"Персонально:\n" +
              $"  Таблеток: {pd.Tablets}\n" +
              $"  Дата покупки: {pd.PurchaseDate:dd.MM.yyyy}\n" +
              $"  Активно: {pd.IsActive}\n" +
              new string('-', 50)
          );
        }

        return personalDrugs;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка клиента: {ex.Message}");
        return new List<PersonalDrug>();
      }
    }

    /// <summary>
    /// Добавление лекарства пользователю.
    /// </summary>
    public async Task AddPersonalDrugs(long userId, Drug drug)
    {
      var newPersonalDrug = new ServerPersonal
      {
        IdUser = userId,
        IdDrug = drug.Id,
        Tablets = drug.TabletsInPack,
        DataBuy = DateTime.Now.ToString("dd.MM.yyyy"),
        Active = false,
      };

      var json = JsonSerializer.Serialize(newPersonalDrug);
      var content = new StringContent(json, Encoding.UTF8, "application/json");

      using var client = new HttpClient();
      var response = await client.PostAsync("http://localhost:5000/api/?action=personal", content);

      var result = await response.Content.ReadAsStringAsync();
      Console.WriteLine(result);
    }

    /// <summary>
    /// Изменение количества таблеток лекарства пользователя.
    /// </summary>
    /// <param name="personaId">Id в таблице personal_drug.</param>
    /// <param name="tablets">Новое количество таблеток.</param>
    public async Task UpdatePersonalDrugsTablets(int personaId, int tablets)
    {
      using var client = new HttpClient();
      string url = $"http://localhost:5000/api/?action=updatepersonal&id={personaId}&tablets={tablets}";

      var response = await client.PostAsync(url, null);
      var result = await response.Content.ReadAsStringAsync();
      Console.WriteLine(result);
    }

    /// <summary>
    /// Изменение активности пользовательского лекарства.
    /// </summary>
    /// <param name="personaId">Id в таблице personal_drug.</param>
    /// <param name="isActive">Новый статус активности.</param>
    public async Task UpdatePersonalDrugActive(int personaId, bool isActive)
    {
      using var client = new HttpClient();

      string url = $"http://localhost:5000/api/?action=updateactive&id={personaId}&active={isActive}";

      var response = await client.PostAsync(url, null);

      var result = await response.Content.ReadAsStringAsync();
      Console.WriteLine(result);
    }

    /// <summary>
    /// Удаление лекарства у пользователя.
    /// </summary>
    /// <param name="personalDrugId">Id в таблице personal_drug.</param>
    public async Task DeletePersonalDrug(int personalDrugId)
    {
      using var client = new HttpClient();

      string url = $"http://localhost:5000/api/?action=deletePersonal&id={personalDrugId}";

      var response = await client.PostAsync(url, null);

      var result = await response.Content.ReadAsStringAsync();
      Console.WriteLine(result);
    }
  }
}
