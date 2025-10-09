using System;
using System.Collections.Generic;
using System.Linq;

namespace PTL
{
    /// <summary>
    /// Extensions utilitaires pour CSV
    /// </summary>
    public static class CsvExtensions
    {
        /// <summary>
        /// Détermine le délimiteur probable d’une première ligne de CSV.
        /// Usage: char delim = firstLine.GuessCsvDelimiter();
        /// </summary>
        public static char GuessCsvDelimiter(this string? firstLine)
        {
            if (string.IsNullOrEmpty(firstLine)) return ',';

            int commas = firstLine.Count(c => c == ',');
            int semis = firstLine.Count(c => c == ';');
            int tabs = firstLine.Count(c => c == '\t');

            if (semis >= commas && semis >= tabs) return ';';
            if (tabs >= commas && tabs >= semis) return '\t';
            return ','; // défaut
        }
    }

    /// <summary>
    /// Extensions de requêtage pour la liste de températures
    /// </summary>
    public static class TemperatureQueryExtensions
    {
        /// <summary>
        /// Filtre par villes (insensible à la casse) et plage d’années [yearFrom..yearTo].
        /// Passer une liste vide/null pour "toutes les villes".
        /// Usage: var data = _records.FilterByCitiesAndYears(cities, 1990, 2020);
        /// </summary>
        public static IEnumerable<Form1.Temperature> FilterByCitiesAndYears(
            this IEnumerable<Form1.Temperature> source,
            IEnumerable<string>? cities,
            int yearFrom,
            int yearTo)
        {
            var set = (cities ?? Array.Empty<string>())
                      .Where(s => !string.IsNullOrWhiteSpace(s))
                      .ToHashSet(StringComparer.OrdinalIgnoreCase);

            bool allCities = set.Count == 0;

            return source.Where(r =>
                (allCities || set.Contains(r.City)) &&
                r.Year >= yearFrom && r.Year <= yearTo);
        }
    }
}
