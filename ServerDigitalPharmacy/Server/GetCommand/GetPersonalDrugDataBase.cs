using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Server.TableClass;

namespace Server.GetCommand
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
          IdUser = reader.IsDBNull(userIndex) ? 0 : reader.GetInt64(userIndex),
          IdDrug = reader.IsDBNull(drugIndex) ? 0 : reader.GetInt32(drugIndex),
          Tablets = reader.IsDBNull(tabletsIndex) ? 0 : reader.GetInt32(tabletsIndex),
          DataBuy = reader.IsDBNull(dateIndex) ? string.Empty : reader.GetString(dateIndex),
          Active = !reader.IsDBNull(activeIndex) && reader.GetBoolean(activeIndex)
        });
      }

      return items;
    }

    public static async Task<List<PersonalDrug>> GetPersonalDrugById(int id_user, string ConnectionString)
    {
      var items = new List<PersonalDrug>();
      using var connection = new MySqlConnection(ConnectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand(
          "SELECT id, id_user, id_drug, tablets, data_buy, active FROM personal_drugs WHERE id_user = @id_user",
          connection);
      //cmd.Parameters.AddWithValue("@id", id);
      cmd.Parameters.AddWithValue("@id_user", id_user);

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
          IdUser = reader.IsDBNull(userIndex) ? 0 : reader.GetInt64(userIndex),
          IdDrug = reader.IsDBNull(drugIndex) ? 0 : reader.GetInt32(drugIndex),
          Tablets = reader.IsDBNull(tabletsIndex) ? 0 : reader.GetInt32(tabletsIndex),
          DataBuy = reader.IsDBNull(dateIndex) ? string.Empty : reader.GetString(dateIndex),
          Active = !reader.IsDBNull(activeIndex) && reader.GetBoolean(activeIndex)
        });
      }

      return items;
    }
  }
}
