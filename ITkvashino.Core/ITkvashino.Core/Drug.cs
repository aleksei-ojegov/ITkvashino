using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITkvashino.Core
{
    /// <summary>
    /// Класс лекарства
    /// </summary>
    public class Drug
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        public long TelegramUserId { get; set; }
        /// <summary>
        /// Наименование лекарства
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Срок годности (в годах/месяцах или точной датой)
        /// </summary>
        public string ShelfLife { get; set; } = string.Empty;
        /// <summary>
        /// Дата покупки
        /// </summary>
        public DateTime PurchaseDate { get; set; }
        /// <summary>
        /// Количество таблеток в упаковке
        /// </summary>
        public int TabletsInPack { get; set; }
        /// <summary>
        /// Дозировка
        /// </summary>
        public Dosage Dosage { get; set; } = new Dosage();
        /// <summary>
        /// Показания к приминению
        /// </summary>
        public string Indications { get; set; } = string.Empty;
        /// <summary>
        /// Фармако-терапевтическая группа
        /// </summary>
        public PharmacotherapeuticGroup Group { get; set; }
    }
}