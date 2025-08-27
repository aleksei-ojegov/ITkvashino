using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Server
{
  public class GetPersonalDrugDataBase
  {
    public static async Task<List<PersonalDrug>> GetAllPersonalDrugs(string ConnectionString)
    {
      var items = new List<PersonalDrug>();

      using var connection = new MySqlConnection(ConnectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand(
          "SELECT id, id_user, id_drug, tablets, data_buy, active FROM personal_drugs ORDER BY id",
          connection);

      using var reader = await cmd.ExecuteReaderAsync();

      int idIndex = reader.GetOrdinal("id");
      int userIndex = reader.GetOrdinal("id_user");
      int drugIndex = reader.GetOrdinal("id_drug");
      int tabletsIndex = reader.GetOrdinal("tablets");
      int dateIndex = reader.GetOrdinal("data_buy");
      int activeIndex = reader.GetOrdinal("active");

      while (await reader.ReadAsync())
      {
        items.Add(new PersonalDrug
        {
          Id = reader.IsDBNull(idIndex) ? 0 : reader.GetInt32(idIndex),
          IdUser = reader.IsDBNull(userIndex) ? 0 : reader.GetInt32(userIndex),
          IdDrug = reader.IsDBNull(drugIndex) ? 0 : reader.GetInt32(drugIndex),
          Tablets = reader.IsDBNull(tabletsIndex) ? 0 : reader.GetInt32(tabletsIndex),
          DataBuy = reader.IsDBNull(dateIndex) ? string.Empty : reader.GetString(dateIndex),
          Active = !reader.IsDBNull(activeIndex) && reader.GetBoolean(activeIndex)
        });
      }

      return items;
    }

    public static async Task<PersonalDrug?> GetPersonalDrugById(int id, string ConnectionString)
    {
      using var connection = new MySqlConnection(ConnectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand(
          "SELECT id, id_user, id_drug, tablets, data_buy, active FROM personal_drugs WHERE id = @id",
          connection);
      cmd.Parameters.AddWithValue("@id", id);

      using var reader = await cmd.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        int idIndex = reader.GetOrdinal("id");
        int userIndex = reader.GetOrdinal("id_user");
        int drugIndex = reader.GetOrdinal("id_drug");
        int tabletsIndex = reader.GetOrdinal("tablets");
        int dateIndex = reader.GetOrdinal("data_buy");
        int activeIndex = reader.GetOrdinal("active");

        return new PersonalDrug
        {
          Id = reader.IsDBNull(idIndex) ? 0 : reader.GetInt32(idIndex),
          IdUser = reader.IsDBNull(userIndex) ? 0 : reader.GetInt32(userIndex),
          IdDrug = reader.IsDBNull(drugIndex) ? 0 : reader.GetInt32(drugIndex),
          Tablets = reader.IsDBNull(tabletsIndex) ? 0 : reader.GetInt32(tabletsIndex),
          DataBuy = reader.IsDBNull(dateIndex) ? string.Empty : reader.GetString(dateIndex),
          Active = !reader.IsDBNull(activeIndex) && reader.GetBoolean(activeIndex)
        };
      }

      return null;
    }
  }
}
