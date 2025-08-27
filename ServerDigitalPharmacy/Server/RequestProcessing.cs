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

              if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out int id))
              {
                var p = await GetPersonalDrugDataBase.GetPersonalDrugById(id, ConnectionString);
                personal = p != null ? new List<PersonalDrug> { p } : new List<PersonalDrug>();
              }
              else
              {
                personal = await GetPersonalDrugDataBase.GetAllPersonalDrugs(ConnectionString);
              }

              await WriteXmlResponse(response, personal);
              break;
            }
            break;

          default:
            SendError(response, $"Неизвестное действие: {action}", 400);
            break;
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
  }
}

