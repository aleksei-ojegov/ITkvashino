using System;
using System.Collections.Generic;
using System.IO; 
using System.Net;
using System.Text; 
using System.Threading.Tasks; 
using System.Xml.Linq; 
using MySql.Data.MySqlClient; 
using Org.BouncyCastle.Crypto; 

class Program 
{ 
    private static readonly string ConnectionString = "Server=localhost;Port=3306;Database=myapp_db;User ID=root;Password=12345;SslMode=None;"; 
    
    static async Task Main(string[] args) 
    { 
        if (!await IsDatabaseAvailable()) 
        { 
            Console.WriteLine("Ошибка: не удалось подключиться к MySQL."); 
            return; 
        } 

        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/api/"); 
        listener.Start(); 
        Console.WriteLine("Сервер запущен на http://localhost:5000/api/");

        try 
        { 
            while (true) 
            { 
                var context = await listener.GetContextAsync();
                await HandleRequest(context); 
            } 
        } 
        catch (Exception ex) 
        { 
            Console.WriteLine($"Критическая ошибка: {ex.Message}"); 
        } 
        finally 
        { 
            listener.Stop(); 
            listener.Close(); 
        } 
    }

    static async Task HandleRequest(HttpListenerContext context) 
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
                if (kv.Length == 2) 
                    queryParams[kv[0]] = kv[1]; 
            } 

            var idStr = queryParams.GetValueOrDefault("id"); 
            if (string.IsNullOrEmpty(idStr)) 
            { 
                SendError(response, "Параметр 'id' обязателен", 400); 
                return; 
            } 
            
            if (!int.TryParse(idStr, out int id)) 
            { 
                SendError(response, "Параметр 'id' должен быть целым числом", 400); 
                return; 
            } 
            
            var data = await GetDataFromDatabase(id); 
            var xml = new XDocument( 
                new XElement("response", 
                new XElement("status", "success"), 
                new XElement("data", 
                    data.ConvertAll(d => new XElement("item", 
                        new XElement("id", d.Id), 
                        new XElement("name", d.Name), 
                        new XElement("value", d.Value)
                        )) 
                    ) 
                ) 
            ); 
                        
            var xmlString = xml.ToString(); 
            var buffer = Encoding.UTF8.GetBytes(xmlString); 
            response.ContentType = "application/xml; charset=utf-8"; 
            response.ContentLength64 = buffer.Length; 

            using (response.OutputStream) 
            { 
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length); 
            } 
        } 
        catch (Exception ex) 
        { 
            SendError(response, $"Ошибка: {ex.Message}", 500); 
        } 
    } 
    
    static async Task<List<DataItem>> GetDataFromDatabase(int id)
    {
        var items = new List<DataItem>();

        using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        using var cmd = new MySqlCommand("SELECT id, name, value FROM items WHERE id = @id", connection);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();

        int idIndex = reader.GetOrdinal("id");
        int nameIndex = reader.GetOrdinal("name");
        int valueIndex = reader.GetOrdinal("value");

        while (await reader.ReadAsync())
        {
            items.Add(new DataItem
            {
                Id = reader.IsDBNull(idIndex) ? 0 : reader.GetInt32(idIndex),
                Name = reader.IsDBNull(nameIndex) ? string.Empty : reader.GetString(nameIndex),
                Value = reader.IsDBNull(valueIndex) ? null : reader.GetString(valueIndex) // оставим null, если в БД null
            });
        }

        return items;
    }
    
    static void SendError(HttpListenerResponse response, string message, int statusCode) 
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
    
    static async Task<bool> IsDatabaseAvailable() 
    { 
        try 
        { 
            using var connection = new MySqlConnection(ConnectionString); 
            await connection.OpenAsync(); 
            return true; 
        } 
        catch 
        { 
            return false; 
        } 
    } 
    
} 

public class DataItem 
{ 
    public int Id { get; set; } 
    public string Name { get; set; } 
    public string Value { get; set; } 
}
