using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Xml.Linq;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Server.TableClass;
using Server.GetCommand;
using System.IO;
using System.Text.Json;
using Server.SetCommand;

namespace Server
{
  public static class RequestProcessing
  {
    /// <summary>
    /// Получает GET запрос от пользователя и подключает метод ответа в форме XML
    /// </summary>
    /// <param name="context">Запрос пользователя по HTTP.</param>
    /// <param name="ConnectionString">Ссылка на БД.</param>
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

        var action = queryParams.GetValueOrDefault("action")?.ToLower() ?? "users"; 
        var idStr = queryParams.GetValueOrDefault("id");

        if (request.HttpMethod == "POST" && action == "personal")
        {
          using var reader = new StreamReader(request.InputStream);
          var body = await reader.ReadToEndAsync();

          var pd = JsonSerializer.Deserialize<PersonalDrug>(body);
          if (pd == null)
          {
            SendError(response, "Неверные данные", 400);
            return;
          }

          await SetPersonalDrug.AddPersonalDrug(pd, ConnectionString);

          await WriteXmlResponse(response, new List<PersonalDrug> { pd });
          return;
        }

        if (request.HttpMethod == "POST" && action == "updatePersonal")
        {
          using var reader = new StreamReader(request.InputStream);
          var body = await reader.ReadToEndAsync();

          var pd = JsonSerializer.Deserialize<PersonalDrug>(body);
          if (pd == null || pd.Id == 0)
          {
            SendError(response, "Неверные данные", 400);
            return;
          }

          await SetPersonalDrug.UpdatePersonalDrugTablets(pd.Id, pd.Tablets, ConnectionString);

          await WriteXmlResponse(response, new List<PersonalDrug> { pd });
          return;
        }

        if (request.HttpMethod == "POST" && action == "updateActive")
        {
          using var reader = new StreamReader(request.InputStream);
          var body = await reader.ReadToEndAsync();

          var pd = JsonSerializer.Deserialize<PersonalDrug>(body);
          if (pd == null || pd.Id == 0)
          {
            SendError(response, "Неверные данные", 400);
            return;
          }

          await SetPersonalDrug.UpdatePersonalDrugActive(pd.Id, pd.Active, ConnectionString);

          await WriteXmlResponse(response, new List<PersonalDrug> { pd });
          return;
        }

        if (request.HttpMethod == "POST" && action == "deletePersonal")
        {
          using var reader = new StreamReader(request.InputStream);
          var body = await reader.ReadToEndAsync();

          var pd = JsonSerializer.Deserialize<PersonalDrug>(body);
          if (pd == null || pd.Id == 0)
          {
            SendError(response, "Неверные данные. Требуется Id записи для удаления", 400);
            return;
          }

          try
          {
            await SetPersonalDrug.DeletePersonalDrugById(pd.Id, ConnectionString);
            await WriteXmlResponse(response, new List<PersonalDrug> { pd });
          }
          catch (Exception ex)
          {
            SendError(response, $"Ошибка при удалении: {ex.Message}", 500);
          }

          return;
        }

        switch (action)
        {
          case "users":
            {
              List<User> items;

              if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out int id))
                items = await GetUserDataBase.GetDataFromDatabase(id, ConnectionString);
              else
                items = await GetUserDataBase.GetAllDataFromDatabase(ConnectionString);

              await WriteXmlResponse(response, items);
              break;
            }
          case "drugs":
            {
              List<Drug> drugs;

              if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out int id))
              {
                var drug = await GetDrugDataBase.GetDrugById(id, ConnectionString);
                drugs = drug != null ? new List<Drug> { drug } : new List<Drug>();
              }
              else
              {
                drugs = await GetDrugDataBase.GetAllDrugs(ConnectionString);
              }

              await WriteXmlResponse(response, drugs);
              break;
            }
          case "personal":
            {
              List<PersonalDrug> personal;

              var allowedKeys = new HashSet<string> { "action", "id_user" };
              foreach (var key in queryParams.Keys)
              {
                if (!allowedKeys.Contains(key))
                {
                  SendError(response, $"Неизвестный параметр: {key}", 400);
                  return;
                }
              }

              var idUserStr = queryParams.GetValueOrDefault("id_user");

              if (!string.IsNullOrWhiteSpace(idUserStr) && int.TryParse(idUserStr, out int userId))
              {
                personal = await GetPersonalDrugDataBase.GetPersonalDrugById(userId, ConnectionString);
              }
              else
              {
                personal = await GetPersonalDrugDataBase.GetAllPersonalDrugs(ConnectionString);
              }

              await WriteXmlResponse(response, personal);
              break;
            }
          case "updatepersonal":
            {
              var tabletsStr = queryParams.GetValueOrDefault("tablets");

              if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out int id) &&
                  !string.IsNullOrWhiteSpace(tabletsStr) && int.TryParse(tabletsStr, out int newTablets))
              {
                try
                {
                  await SetPersonalDrug.UpdatePersonalDrugTablets(id, newTablets, ConnectionString);
                  SendSuccess(response, "Количество таблеток обновлено");
                }
                catch (Exception ex)
                {
                  SendError(response, $"Ошибка при обновлении: {ex.Message}", 500);
                }
              }
              else
              {
                SendError(response, "Неверные параметры запроса. Требуются id и tablets", 400);
              }

              break;
            }
          case "updateactive":
            {
              var activeStr = queryParams.GetValueOrDefault("active");

              if (string.IsNullOrWhiteSpace(idStr) || string.IsNullOrWhiteSpace(activeStr)
                  || !int.TryParse(idStr, out int id) || !bool.TryParse(activeStr, out bool isActive))
              {
                SendError(response, "Неверные параметры запроса. Требуются id и active", 400);
                break;
              }

              await SetPersonalDrug.UpdatePersonalDrugActive(id, isActive, ConnectionString);
              SendSuccess(response, "Лекарство установило новый статус.");
              break;
            }
          case "deletepersonal":
            {
              if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out int id))
              {
                SendError(response, "Неверные параметры запроса. Требуется id", 400);
                break;
              }

              try
              {
                await SetPersonalDrug.DeletePersonalDrugById(id, ConnectionString);
                SendSuccess(response, $"Запись с id={id} успешно удалена.");
              }
              catch (Exception ex)
              {
                SendError(response, $"Ошибка при удалении: {ex.Message}", 500);
              }

              break;
            }

          default:
            SendError(response, $"Неизвестное действие: {action}", 400);
            break;
        }
      }
      catch (Exception ex)
      {
       Console.WriteLine($"[ServerError] {ex}");
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

    private static void SendSuccess(HttpListenerResponse response, string message)
    {
      response.ContentType = "application/xml; charset=utf-8";
      response.StatusCode = 200;

      using var writer = new StreamWriter(response.OutputStream);
      writer.Write(
          $"<response>\n" +
          $"  <status>success</status>\n" +
          $"  <message>{System.Security.SecurityElement.Escape(message)}</message>\n" +
          $"</response>");
    }
  }
}

