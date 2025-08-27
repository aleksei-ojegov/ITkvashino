using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
  /// <summary>
  /// Шаблон для элемента таблицы пользователя
  /// </summary>
  public class User
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public long ChatId { get; set; }
  }
}
