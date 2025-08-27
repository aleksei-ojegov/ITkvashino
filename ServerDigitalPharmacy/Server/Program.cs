using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Crypto;

namespace Server
{
  class Program
  {
    /// <summary>
    /// Путь для подключение к БД
    /// </summary>
    private static readonly string ConnectionString = "Server=localhost;Port=3306;Database=Drug;User ID=root;Password=;SslMode=None;";

    static async Task Main(string[] args)
    {
      if (!await IsDatabaseAvailable())
      {
        Console.WriteLine("Ошибка: не удалось подключиться к MySQL.");
        return;
      }

      /// <summary>
      /// Проверка на подключение к БД
      /// </summary>
      /// <returns>True, если проверка успешна. Иначе - false.</returns>
      static async Task<bool> IsDatabaseAvailable()
      {
        try
        {
          using var connection = new MySqlConnection(ConnectionString);
          await connection.OpenAsync();
          return true;
        }
        catch (Exception ex)
        {
          Console.WriteLine($"DB connection failed: {ex.GetType().Name}: {ex.Message}");
          Console.WriteLine(ex.StackTrace);
          return false;
        }
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
          await RequestProcessing.HandleRequest(context, ConnectionString);
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
  }
}

