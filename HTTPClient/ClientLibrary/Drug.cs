using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary
{
  public class Drug
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShelfLife { get; set; } = string.Empty;
    public int TabletsInPack { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public string Indications { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
  }
}
