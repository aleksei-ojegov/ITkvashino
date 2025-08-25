using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ClientLibrary
{
  public class SendURLandParseXml
  {
    public static async Task<string> GetStringAsync(string url)
    {
      using var http = new HttpClient();
      var resp = await http.GetAsync(url);
      resp.EnsureSuccessStatusCode();
      return await resp.Content.ReadAsStringAsync();
    }

    public static List<Drug> ParseDrugs(string xml)
    {
      var doc = XDocument.Parse(xml);

      var status = doc.Root?.Element("status")?.Value?.Trim().ToLowerInvariant();
      if (status == "error")
      {
        var msg = doc.Root?.Element("message")?.Value ?? "Неизвестная ошибка";
        throw new InvalidOperationException($"Сервер вернул ошибку: {msg}");
      }
      if (status != "success")
      {
        throw new InvalidOperationException("Неверный формат XML: status != success");
      }

      var data = doc.Root?.Element("data");
      if (data == null)
        throw new InvalidOperationException("Неверный формат XML: отсутствует <data>");

      var result = new List<Drug>();
      foreach (var xDrug in data.Elements("drug"))
      {
        result.Add(new Drug
        {
          Id = GetInt(xDrug, "id"),
          Name = GetString(xDrug, "name"),
          Description = GetString(xDrug, "description"),
          ShelfLife = GetString(xDrug, "shelfLife"),
          TabletsInPack = GetInt(xDrug, "tabletsInPack"),
          Dosage = GetString(xDrug, "dosage"),
          Indications = GetString(xDrug, "indications"),
          Group = GetString(xDrug, "group"),
        });
      }

      return result;
    }

    public static List<User> ParseUsers(string xml)
    {
      var xdoc = XDocument.Parse(xml);

      var users = new List<User>();
      foreach (var elem in xdoc.Descendants("user"))
      {
        users.Add(new User
        {
          Id = (int)elem.Element("id"),
          Name = (string)elem.Element("name"),
          ChatId = (long)elem.Element("chatId")
        });
      }

      return users;
    }

    // Утилиты для безопасного чтения элементов
    public static string GetString(XElement parent, string name)
        => parent.Element(name)?.Value ?? string.Empty;

    public static int GetInt(XElement parent, string name)
    {
      var s = parent.Element(name)?.Value;
      return int.TryParse(s, out var v) ? v : 0;
    }
  }

}
