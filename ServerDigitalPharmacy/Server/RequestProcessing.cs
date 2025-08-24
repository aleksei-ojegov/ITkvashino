using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Xml.Linq;
using System.Data;

namespace Server
{
  public static class RequestProcessing
  {
    /// <summary>
    /// Получает запрос от пользователя и формирует ответ в формате XML
    /// </summary>
    /// <param name="context">Запрос пользователя по HTTP.</param>
    public static async Task HandleRequest(HttpListenerContext context, string ConnectionString)
    {
      var request = context.Request;
      var response = context.Response;

      try
      {
        var query = System.Net.WebUtility.UrlDecode(request.Url.Query.TrimStart('?'));
        var queryParams = new Dictionary<string, string>();
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
          var kv = pair.Split('=', 2);
          if (kv.Length == 2) queryParams[kv[0]] = kv[1];
        }

        var action = queryParams.GetValueOrDefault("action")?.ToLower() ?? "users"; // по умолчанию users
        var idStr = queryParams.GetValueOrDefault("id");

        if (action == "users")
        {
          List<DataItem> items;

          if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out int id))
            items = await GetDataFromDatabase(id, ConnectionString);
          else
            items = await GetAllDataFromDatabase(ConnectionString); // метод для всех пользователей

          await WriteXmlResponse(response, items);
        }
        else if (action == "drugs")
        {
          List<Drug> drugs;

          if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out int id))
          {
            var drug = await GetDrugById(id, ConnectionString);
            drugs = drug != null ? new List<Drug> { drug } : new List<Drug>();
          }
          else
          {
            drugs = await GetAllDrugs(ConnectionString);
          }

          await WriteXmlResponse(response, drugs);
        }
        else
        {
          SendError(response, $"Неизвестное действие: {action}", 400);
        }
      }
      catch (Exception ex)
      {
        SendError(response, $"Ошибка: {ex.Message}", 500);
      }
    }

    /// <summary>
    /// Общий метод для формирования XML и отправки в ответ
    /// </summary>
    /// <param name="response">Запрос пользователя по HTTP.</param>
    /// <param name="data">Все объекты класса полученые в ответ на запрос.</param>
    private static async Task WriteXmlResponse<T>(HttpListenerResponse response, List<T> data)
    {
      var xml = new XDocument(
          new XElement("response",
              new XElement("status", "success"),
              new XElement("data",
                  data.ConvertAll(d =>
                  {
                    var elem = new XElement(typeof(T).Name.ToLower());
                    foreach (var prop in typeof(T).GetProperties())
                    {
                      var value = prop.GetValue(d)?.ToString() ?? string.Empty;
                      elem.Add(new XElement(prop.Name.Substring(0, 1).ToLower() + prop.Name.Substring(1), value));
                    }
                    return elem;
                  })
              )
          )
      );

      var buffer = Encoding.UTF8.GetBytes(xml.ToString());
      response.ContentType = "application/xml; charset=utf-8";
      response.ContentLength64 = buffer.Length;
      using (response.OutputStream)
      {
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
      }
    }

    /// <summary>
    /// Получает запрос от пользователя и формирует ответ в формате XML
    /// для найденого элемента БД.
    /// </summary>
    /// <param name="id">Query-параметры запроса из URL.</param>
    /// <param name="ConnectionString">Ссылка на таблицу БД.</param>
    /// <returns>Преобразует каждую строчку в БД в объект класса.</returns>
    public static async Task<List<DataItem>> GetDataFromDatabase(int id, string ConnectionString)
    {
      var items = new List<DataItem>();

      using var connection = new MySqlConnection(ConnectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand("SELECT id, name, chat_id FROM users WHERE id = @id", connection);
      cmd.Parameters.AddWithValue("@id", id);

      using var reader = await cmd.ExecuteReaderAsync();

      int idIndex = reader.GetOrdinal("id");
      int nameIndex = reader.GetOrdinal("name");
      int valueIndex = reader.GetOrdinal("chat_id");

      while (await reader.ReadAsync())
      {
        items.Add(new DataItem
        {
          Id = reader.IsDBNull(idIndex) ? 0 : reader.GetInt32(idIndex),
          Name = reader.IsDBNull(nameIndex) ? string.Empty : reader.GetString(nameIndex),
          ChatId = reader.IsDBNull(valueIndex) ? 0 : reader.GetInt32(valueIndex)
        });
      }

      return items;
    }

    /// <summary>
    /// Получает запрос от пользователя и формирует ответ в формате XML
    /// для всех элементов таблицы БД.
    /// </summary>
    /// <param name="ConnectionString">Ссылка на таблицу БД.</param>
    /// <returns>Преобразует каждую строчку в БД в объект класса.</returns>
    static async Task<List<DataItem>> GetAllDataFromDatabase(string ConnectionString)
    {
      var items = new List<DataItem>();

      using var connection = new MySqlConnection(ConnectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand("SELECT id, name, chat_id FROM users ORDER BY id", connection);
      using var reader = await cmd.ExecuteReaderAsync();

      int idIndex = reader.GetOrdinal("id");
      int nameIndex = reader.GetOrdinal("name");
      int valueIndex = reader.GetOrdinal("chat_id");

      while (await reader.ReadAsync())
      {
        items.Add(new DataItem
        {
          Id = reader.IsDBNull(idIndex) ? 0 : reader.GetInt32(idIndex),
          Name = reader.IsDBNull(nameIndex) ? string.Empty : reader.GetString(nameIndex),
          ChatId = reader.IsDBNull(valueIndex) ? 0 : reader.GetInt32(valueIndex)
        });
      }

      return items;
    }

    /// <summary>
    /// Получает запрос от пользователя и формирует ответ в формате XML
    /// для найденого элемента БД.
    /// </summary>
    /// <param name="id">Query-параметры запроса из URL.</param>
    /// <param name="connectionString">Ссылка на таблицу БД.</param>
    /// <returns>Преобразует каждую строчку в БД в объект класса.</returns>
    public static async Task<Drug?> GetDrugById(int id, string connectionString)
    {
      using var connection = new MySqlConnection(connectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand(
          "SELECT id, name, description, shelf_life, tablets, dosage, indication, `groups` FROM drugs WHERE id = @id",
          connection);
      cmd.Parameters.AddWithValue("@id", id);

      using var reader = await cmd.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        return new Drug
        {
          Id = reader.GetInt32("id"),
          Name = reader.GetString("name"),
          Description = reader.GetString("description"),
          ShelfLife = reader.GetString("shelf_life"),
          TabletsInPack = reader.GetInt32("tablets"),
          Dosage = reader.GetString("dosage"),
          Indications = reader.GetString("indication"),
          Group = reader.GetString("groups")
        };
      }

      return null;
    }

    /// <summary>
    /// Получает запрос от пользователя и формирует ответ в формате XML
    /// для всех элементов таблицы БД.
    /// </summary>
    /// <param name="connectionString">Ссылка на таблицу БД.</param>
    /// <returns>Преобразует каждую строчку в БД в объект класса.</returns>
    public static async Task<List<Drug>> GetAllDrugs(string connectionString)
    {
      var drugs = new List<Drug>();

      using var connection = new MySqlConnection(connectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand(
          "SELECT id, name, description, shelf_life, tablets, dosage, indication, `groups` FROM drugs",
          connection);

      using var reader = await cmd.ExecuteReaderAsync();

      while (await reader.ReadAsync())
      {
        drugs.Add(new Drug
        {
          Id = reader.GetInt32("id"),
          Name = reader.GetString("name"),
          Description = reader.GetString("description"),
          ShelfLife = reader.GetString("shelf_life"),
          TabletsInPack = reader.GetInt32("tablets"),
          Dosage = reader.GetString("dosage"),
          Indications = reader.GetString("indication"),
          Group = reader.GetString("groups")
        });
      }

      return drugs;
    }

    /// <summary>
    /// Формирует XML-ответ с ошибкой
    /// </summary>
    /// <param name="response">Query-параметры запроса из URL.</param>
    /// <param name="message">Сообщение ошибки.</param>
    /// <param name="statusCode">Код ошибки.</param>
    public static void SendError(HttpListenerResponse response, string message, int statusCode)
    {
      var errorXml = new XDocument(
          new XElement("response",
          new XElement("status", "error"),
          new XElement("message", message)
          )
      );

      var buffer = Encoding.UTF8.GetBytes(errorXml.ToString());
      response.StatusCode = statusCode;
      response.ContentType = "application/xml; charset=utf-8";
      response.ContentLength64 = buffer.Length;

      using (response.OutputStream)
      {
        response.OutputStream.Write(buffer, 0, buffer.Length);
      }
    }

    /// <summary>
    /// Шаблон для элемента таблицы пользователя
    /// </summary>
    public class DataItem
    {
      public int Id { get; set; }
      public string Name { get; set; }
      public long ChatId { get; set; }
    }

    /// <summary>
    /// Шаблон для лекарств
    /// </summary>
    public class Drug
    {
      /// <summary>
      /// Идентификатор
      /// </summary>
      public int Id { get; set; }

      /// <summary>
      /// Наименование лекарства
      /// </summary>
      public string Name { get; set; } = string.Empty;

      /// <summary>
      /// Описание
      /// </summary>
      public string Description { get; set; } = string.Empty;

      /// <summary>
      /// Срок годности (в годах/месяцах или точной датой)
      /// </summary>
      public string ShelfLife { get; set; } = string.Empty;

      /// <summary>
      /// Количество таблеток в упаковке
      /// </summary>
      public int TabletsInPack { get; set; }
      /// <summary>
      /// Дозировка
      /// </summary>
      public string Dosage { get; set; }
      /// <summary>
      /// Показания к приминению
      /// </summary>
      public string Indications { get; set; } = string.Empty;
      /// <summary>
      /// Фармако-терапевтическая группа
      /// </summary>
      public string Group { get; set; }
    }
  }
}

