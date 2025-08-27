using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITkvashino.Core;
using System.Data;
using MySql.Data.MySqlClient;

namespace TGbot
{


    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task SaveUserAsync(long chatId, string name)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new MySqlCommand(
                "INSERT INTO users (chat_id, name) VALUES (@chatId, @name) " +
                "ON DUPLICATE KEY UPDATE name = @name", connection);

            cmd.Parameters.AddWithValue("@chatId", chatId);
            cmd.Parameters.AddWithValue("@name", name);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Drug>> GetAllDrugsAsync()
        {
            var drugs = new List<Drug>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new MySqlCommand(
                "SELECT id, name, description, shelf_life, tablets, dosage, indication, `groups` FROM drugs",
                connection);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                drugs.Add(new Drug
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    Description = reader.GetString("description"),
                    ShelfLife = reader.GetString("shelf_life"),
                    Dosage = DosageParser.FromString(reader.GetString("dosage")),
                    TabletsInPack = reader.GetInt32("tablets"),
                    Indications = reader.GetString("indication"),
                    Group = Enum.Parse<PharmacotherapeuticGroup>(reader.GetString("groups"))
                });
            }

            return drugs;
        }
    }
}
