using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Server.TableClass;

namespace Server.GetCommand
{
  public class GetDrugDataBase
  {
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
        int idIndex = reader.GetOrdinal("id");
        int nameIndex = reader.GetOrdinal("name");
        int descIndex = reader.GetOrdinal("description");
        int shelfIndex = reader.GetOrdinal("shelf_life");
        int tabletsIndex = reader.GetOrdinal("tablets");
        int dosageIndex = reader.GetOrdinal("dosage");
        int indicIndex = reader.GetOrdinal("indication");
        int groupIndex = reader.GetOrdinal("groups");

        return new Drug
        {
          Id = reader.IsDBNull(idIndex) ? 0 : reader.GetInt32(idIndex),
          Name = reader.IsDBNull(nameIndex) ? string.Empty : reader.GetString(nameIndex),
          Description = reader.IsDBNull(descIndex) ? string.Empty : reader.GetString(descIndex),
          ShelfLife = reader.IsDBNull(shelfIndex) ? string.Empty : reader.GetString(shelfIndex),
          TabletsInPack = reader.IsDBNull(tabletsIndex) ? 0 : reader.GetInt32(tabletsIndex),
          Dosage = reader.IsDBNull(dosageIndex) ? string.Empty : reader.GetString(dosageIndex),
          Indications = reader.IsDBNull(indicIndex) ? string.Empty : reader.GetString(indicIndex),
          Group = reader.IsDBNull(groupIndex) ? string.Empty : reader.GetString(groupIndex)
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
      var items = new List<Drug>();

      using var connection = new MySqlConnection(connectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand(
          "SELECT id, name, description, shelf_life, tablets, dosage, indication, `groups` FROM drugs ORDER BY id",
          connection);

      using var reader = await cmd.ExecuteReaderAsync();

      int idIndex = reader.GetOrdinal("id");
      int nameIndex = reader.GetOrdinal("name");
      int descIndex = reader.GetOrdinal("description");
      int shelfIndex = reader.GetOrdinal("shelf_life");
      int tabletsIndex = reader.GetOrdinal("tablets");
      int dosageIndex = reader.GetOrdinal("dosage");
      int indicIndex = reader.GetOrdinal("indication");
      int groupIndex = reader.GetOrdinal("groups");

      while (await reader.ReadAsync())
      {
        items.Add(new Drug
        {
          Id = reader.IsDBNull(idIndex) ? 0 : reader.GetInt32(idIndex),
          Name = reader.IsDBNull(nameIndex) ? string.Empty : reader.GetString(nameIndex),
          Description = reader.IsDBNull(descIndex) ? string.Empty : reader.GetString(descIndex),
          ShelfLife = reader.IsDBNull(shelfIndex) ? string.Empty : reader.GetString(shelfIndex),
          TabletsInPack = reader.IsDBNull(tabletsIndex) ? 0 : reader.GetInt32(tabletsIndex),
          Dosage = reader.IsDBNull(dosageIndex) ? string.Empty : reader.GetString(dosageIndex),
          Indications = reader.IsDBNull(indicIndex) ? string.Empty : reader.GetString(indicIndex),
          Group = reader.IsDBNull(groupIndex) ? string.Empty : reader.GetString(groupIndex)
        });
      }

      return items;
    }
  }
}
