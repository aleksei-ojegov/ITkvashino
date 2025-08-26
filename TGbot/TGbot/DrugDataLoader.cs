using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITkvashino.Core;

namespace TGbot
{
    public class DrugDataLoader
    {
        public List<Drug> LoadDrugsFromFile(string filePath)
        {
            var drugs = new List<Drug>();

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Файл {filePath} не найден!");
                return drugs;
            }

            var lines = File.ReadAllLines(filePath);
            int id = 1;

            foreach (var line in lines)
            {
                var parts = line.Split('_');
                if (parts.Length >= 7)
                {
                    var drug = new Drug
                    {
                        Id = id++,
                        Name = parts[0],
                        Description = parts[1],
                        ShelfLife = parts[2],
                        TabletsInPack = int.Parse(parts[3]),
                        Indications = parts[5],
                        Group = ParsePharmacotherapeuticGroup(parts[6])
                    };
                    drugs.Add(drug);
                }
            }

            return drugs;
        }

        private static PharmacotherapeuticGroup ParsePharmacotherapeuticGroup(string groupText)
        {
            // Простая реализация - можно расширить по необходимости
            return groupText switch
            {
                "Analgesic" => PharmacotherapeuticGroup.Analgesic,
                "Antibiotic" => PharmacotherapeuticGroup.Antibiotic,
                "AntiviralAndImmunostimulant" => PharmacotherapeuticGroup.AntiviralAndImmunostimulant,
                "Antihistamine" => PharmacotherapeuticGroup.Antihistamine,
                "Antipyretic" => PharmacotherapeuticGroup.Antipyretic,
                "Antihypertensiv" => PharmacotherapeuticGroup.Antihypertensiv,
                "GastrointestinalDiseases" => PharmacotherapeuticGroup.GastrointestinalDiseases
            };
        }
    }
}
