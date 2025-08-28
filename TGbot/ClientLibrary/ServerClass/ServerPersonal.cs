using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.ServerClass
{
  public class ServerPersonal
  {
    public long IdUser { get; set; }
    public int IdDrug { get; set; }
    public int Tablets { get; set; }
    public string DataBuy { get; set; }
    public bool Active { get; set; }
  }
}
