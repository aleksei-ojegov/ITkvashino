using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITkvashino.Core;

namespace ClientLibrary
{
    public static class DosageParser
    {
        public static Dosage FromString(string dosageString)
        {
            if (string.IsNullOrWhiteSpace(dosageString))
                throw new ArgumentException("Строка дозировки пуста");

            var parts = dosageString.Split('_');

            if (parts.Length != 3)
                throw new FormatException("Некорректный формат дозировки. Ожидается 'TimesPerDay_TabletsPerIntake_DurationInDays'");

            return new Dosage
            {
                TimesPerDay = int.Parse(parts[0]),
                TabletsPerIntake = int.Parse(parts[1]),
                DurationInDays = int.Parse(parts[2]) == 0 ? null : int.Parse(parts[2])
            };
        }
    }
}
