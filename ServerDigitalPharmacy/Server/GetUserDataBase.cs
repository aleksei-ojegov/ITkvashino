using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Server
{
  public class GetUserDataBase
  {
    /// <summary>
    /// Получает запрос от пользователя и формирует ответ в формате XML
    /// для найденого элемента БД.
    /// </summary>
    /// <param name="id">Query-параметры запроса из URL.</param>
    /// <param name="ConnectionString">Ссылка на таблицу БД.</param>
    /// <returns>Преобразует каждую строчку в БД в объект класса.</returns>
    public static async Task<List<User>> GetDataFromDatabase(int id, string ConnectionString)
    {
      var items = new List<User>();

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
        items.Add(new User
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
    public static async Task<List<User>> GetAllDataFromDatabase(string ConnectionString)
    {
      var items = new List<User>();

      using var connection = new MySqlConnection(ConnectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand("SELECT id, name, chat_id FROM users ORDER BY id", connection);
      using var reader = await cmd.ExecuteReaderAsync();

      int idIndex = reader.GetOrdinal("id");
      int nameIndex = reader.GetOrdinal("name");
      int valueIndex = reader.GetOrdinal("chat_id");

      while (await reader.ReadAsync())
      {
        items.Add(new User
        {
          Id = reader.IsDBNull(idIndex) ? 0 : reader.GetInt32(idIndex),
          Name = reader.IsDBNull(nameIndex) ? string.Empty : reader.GetString(nameIndex),
          ChatId = reader.IsDBNull(valueIndex) ? 0 : reader.GetInt32(valueIndex)
        });
      }

      return items;
    }
  }
}
