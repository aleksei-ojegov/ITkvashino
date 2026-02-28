using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Server.TableClass;

namespace Server.SetCommand
{
  public class SetPersonalDrug
  {
    public static async Task AddPersonalDrug(PersonalDrug pd, string ConnectionString)
    {
      using var connection = new MySqlConnection(ConnectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand(
          @"INSERT INTO personal_drugs (id_user, id_drug, tablets, data_buy, active)
          VALUES (@id_user, @id_drug, @tablets, @data_buy, @active)", connection);

      cmd.Parameters.AddWithValue("@id_user", pd.IdUser);
      cmd.Parameters.AddWithValue("@id_drug", pd.IdDrug);
      cmd.Parameters.AddWithValue("@tablets", pd.Tablets);
      cmd.Parameters.AddWithValue("@data_buy", pd.DataBuy); 
      cmd.Parameters.AddWithValue("@active", pd.Active);

      await cmd.ExecuteNonQueryAsync();
    }

    public static async Task UpdatePersonalDrugTablets(int id, int newTablets, string connectionString)
    {
      using var connection = new MySqlConnection(connectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand(
          "UPDATE personal_drugs SET tablets = @tablets WHERE id = @id", connection);

      cmd.Parameters.AddWithValue("@tablets", newTablets);
      cmd.Parameters.AddWithValue("@id", id);

      await cmd.ExecuteNonQueryAsync();
    }

    public static async Task UpdatePersonalDrugActive(int id, bool isActive, string connectionString)
    {
      using var connection = new MySqlConnection(connectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand(
          "UPDATE personal_drugs SET active = @active WHERE id = @id", connection);

      cmd.Parameters.AddWithValue("@active", isActive);
      cmd.Parameters.AddWithValue("@id", id);

      await cmd.ExecuteNonQueryAsync();
    }

    public static async Task DeletePersonalDrugById(int id, string connectionString)
    {
      using var connection = new MySqlConnection(connectionString);
      await connection.OpenAsync();

      using var cmd = new MySqlCommand(
          "DELETE FROM personal_drugs WHERE id = @id",
          connection);

      cmd.Parameters.AddWithValue("@id", id);

      int rowsAffected = await cmd.ExecuteNonQueryAsync();
      Console.WriteLine(rowsAffected > 0
          ? $"Запись с id={id} успешно удалена."
          : $"Запись с id={id} не найдена.");
    }
  }
}
