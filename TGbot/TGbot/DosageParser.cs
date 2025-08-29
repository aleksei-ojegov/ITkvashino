using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientLibrary;

namespace TGbot
{
    /// <summary>
    /// Класс парсинга дозировки
    /// </summary>
    public static class DosageParser
    {
        #region

        /// <summary>
        /// Парсинг из строки
        /// </summary>
        /// <param name="dosageString">Строка дозировки из БД</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FormatException"></exception>
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

        #endregion
    }
}
