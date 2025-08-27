using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITkvashino.Core
{
    /// <summary>
    /// Класс дозировки лекарства
    /// </summary>
    public class Dosage
    {
        /// <summary>
        /// Количество приёмов в день (например 2 раза)
        /// </summary>
        public int TimesPerDay { get; set; }

        /// <summary>
        /// Количество таблеток за один приём
        /// </summary>
        public int TabletsPerIntake { get; set; }

        /// <summary>
        /// Продолжительность курса лечения (в днях)
        /// </summary>
        public int? DurationInDays { get; set; }

        /// <summary>
        /// Автоматически сгенерированное описание дозировки
        /// </summary>
        public string Description => GenerateDescription();

        /// <summary>
        /// Формирует человекочитаемое описание на основе числовых параметров
        /// </summary>
        private string GenerateDescription()
        {
            string result = $"По {TabletsPerIntake} таблетке(ам) {TimesPerDay} раз(а) в день";

            if (DurationInDays.HasValue)
            {
                result += $". Продолжительность лечения {DurationInDays.Value} дн.";
            }

            return result;
        }
    }
}
