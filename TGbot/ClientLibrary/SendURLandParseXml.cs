using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection.PortableExecutable;
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
          Indications = GetString(xDrug, "indications"),
          Dosage = DosageParser.FromString(GetString(xDrug, "dosage")),
          Group = Enum.Parse<PharmacotherapeuticGroup>(GetString(xDrug,"group")),
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

    public static List<PersonalDrug> ParsePersonalDrugs(string xml)
    {
      var doc = XDocument.Parse(xml);

      var status = doc.Root?.Element("status")?.Value?.Trim().ToLowerInvariant();
      if (status == "error")
      {
        var msg = doc.Root?.Element("message")?.Value ?? "Неизвестная ошибка";
        throw new InvalidOperationException($"Сервер вернул ошибку: {msg}");
      }
      if (status != "success")
        throw new InvalidOperationException("Неверный формат XML: status != success");

      var data = doc.Root?.Element("data");
      if (data == null)
        throw new InvalidOperationException("Неверный формат XML: отсутствует <data>");

      var list = new List<PersonalDrug>();
      foreach (var x in data.Elements("personaldrug"))
      {
        list.Add(new PersonalDrug
        {
          Id = int.Parse(x.Element("id")?.Value ?? "0"),
          IdUser = long.Parse(x.Element("idUser")?.Value ?? "0"),
          IdDrug = int.Parse(x.Element("idDrug")?.Value ?? "0"),
          Tablets = int.Parse(x.Element("tablets")?.Value ?? "0"),
          PurchaseDate = DateTime.ParseExact(
                x.Element("dataBuy")?.Value ?? "01.01.2000",
                "dd.MM.yyyy",
                CultureInfo.InvariantCulture),
          IsActive = bool.Parse(x.Element("active")?.Value ?? "false")
        });
      }

      return list;
    }

    // Утилиты для безопасного чтения элементов
    public static string GetString(XElement parent, string name)
      => parent.Element(name)?.Value ?? string.Empty;

    public static int GetInt(XElement parent, string name)
    {
      var s = parent.Element(name)?.Value;
      return int.TryParse(s, out var v) ? v : 0;
    }
    private static bool GetBool(XElement element, string elementName)
    {
      var value = GetString(element, elementName);
      return bool.TryParse(value, out bool result) && result;
    }

    public static DateTime GetDateTime(XElement parent, string name)
    {
      var str = parent.Element(name)?.Value ?? string.Empty;
      if (DateTime.TryParse(str, out var dt))
        return dt;
      return default; 
    }

    private static long GetLong(XElement parent, string name)
      => long.TryParse(parent.Element(name)?.Value, out var val) ? val : 0;
  }

}
