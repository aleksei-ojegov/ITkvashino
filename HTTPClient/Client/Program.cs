using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using ClientLibrary;

class Program
{
  static async Task Main(string[] args)
  {
    var methods = new Methods(); 
    var drugs = await methods.GetAllDrugs();
    var users = await methods.GetAllUsers();
  }
}
