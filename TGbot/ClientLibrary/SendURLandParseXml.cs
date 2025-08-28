using System;
using System.Collections.Generic;
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
    public static List<PersonalDrug> ParsePersonalDrugs(string xml, List<Drug> drugs, long idTelegramUser)
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
      {
                throw new InvalidOperationException("Неверный формат XML: отсутствует <data>");
      }

      var result = new List<PersonalDrug>();
      foreach (var xDrug in data.Elements("personal"))
      {
        var idDrug = GetInt(xDrug, "id_drug");
        var idUser = GetInt(xDrug, "id_user");
        var drug = drugs.FirstOrDefault(d => d.Id == idDrug);

        if (drug == null)
        {
          throw new InvalidOperationException($"Не найдено лекарство с ID: {idDrug}");
        }
        if (idUser == idTelegramUser)
        {
          var personalDrug = new PersonalDrug
          {
            IdUser = GetInt(xDrug, "id_user"),
            Id = GetInt(xDrug, "id"),
            Tablets = GetInt(xDrug, "tablets"),
            IsActive = GetBool(xDrug, "active"),
            PurchaseDate = GetDateTime(xDrug, "data_buy") ?? DateTime.MinValue,
            IdDrug = idDrug,
            Name = drug.Name,
            Description = drug.Description,
            ShelfLife = drug.ShelfLife,
            TabletsInPack = drug.TabletsInPack,
            Indications = drug.Indications,
            Dosage = drug.Dosage,
            Group = drug.Group
          };

          result.Add(personalDrug);
        }
      }

      return result;
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

    private static DateTime? GetDateTime(XElement element, string elementName)
    {
            var value = GetString(element, elementName);
            if (string.IsNullOrEmpty(value)) return null;

            return DateTime.TryParse(value, out DateTime result) ? result : (DateTime?)null;
    }
  }

}
