using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary
{
    public class PersonalDrug : Drug
    {
        public int IdUser { get; set; }
        public int IdDrug { get; set; }
        public int Tablets { get; set; }
        public bool IsActive { get; set; }
        /// <summary>
        /// Дата покупки
        /// </summary>
        public DateTime PurchaseDate { get; set; }
    }
}
