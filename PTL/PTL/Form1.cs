using CsvHelper;
using CsvHelper.Configuration;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PTL
{
    public partial class Form1 : Form
    {
        // Libellés de mois (répétés sur toute la timeline)
        string[] monthLabels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                 "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        public record Temperature
        {
            public string Region { get; init; } = "";
            public string Country { get; init; } = "";
            public string State { get; init; } = "";
            public string City { get; init; } = "";
            public int Month { get; init; }
            public int Day { get; init; }
            public int Year { get; init; }
            public double AvgTemperature { get; init; }
        }

        private List<Temperature> _records = new();

        // Flag pour éviter de re-tracer pendant les MAJ de listes
        private bool _isUpdatingYears = false;

        public Form1()
        {
            InitializeComponent();

            this.Load += formsPlot1_Load;

            // === CheckedListBox pour villes ===
            // (Assure-toi que le contrôle sur le Form s'appelle "checkedListCities")
            checkedListCities.CheckOnClick = true;
            checkedListCities.ItemCheck += checkedListCities_ItemCheck;

            // Combos années
            comboYearFrom.DropDownStyle = ComboBoxStyle.DropDownList;
            comboYearTo.DropDownStyle = ComboBoxStyle.DropDownList;
            comboYearFrom.SelectedIndexChanged += Years_SelectedIndexChanged;
            comboYearTo.SelectedIndexChanged += Years_SelectedIndexChanged;

            // Selecteur mois/année
            comboGranularity.DropDownStyle = ComboBoxStyle.DropDownList;
            comboGranularity.Items.AddRange(new[] { "Mois", "Année" });
            comboGranularity.SelectedIndex = 0; // par défaut: Mois
            comboGranularity.SelectedIndexChanged += (s, e) => PlotCurrentSelection();
        }

        private void formsPlot1_Load(object? sender, EventArgs e)
        {
            var path = @"C:\Users\pl76tup\Desktop\PTL\PTL\Données\city_temperature.csv";
            if (!File.Exists(path))
            {
                MessageBox.Show($"Fichier introuvable:\n{path}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                BadDataFound = null,
            };

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, cfg);
            _records = csv.GetRecords<Temperature>().ToList();

            if (_records.Count == 0)
            {
                MessageBox.Show("CSV vide.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Remplir la liste des villes (tri alpha)
            var cities = _records.Select(r => r.City)
                                 .Where(s => !string.IsNullOrWhiteSpace(s))
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                                 .ToList();

            checkedListCities.Items.Clear();
            foreach (var c in cities)
                checkedListCities.Items.Add(c, false);

            // Sélection initiale : on coche la première ville si dispo
            if (checkedListCities.Items.Count > 0)
                checkedListCities.SetItemChecked(0, true);

            // Init années selon sélection
            RefreshYearsForSelection();
            PlotCurrentSelection();
        }

        // Quand on coche/décoche une ville (ItemCheck déclenche AVANT maj de l'état)
        private void checkedListCities_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            // On laisse WinForms finir de cocher, puis on met à jour
            BeginInvoke(new Action(() =>
            {
                RefreshYearsForSelection(preserveSelection: true);
                PlotCurrentSelection();
            }));
        }

        // Re-trace quand l'une des bornes d'années change
        private void Years_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingYears) return;
            PlotCurrentSelection();
        }

        // Liste des villes actuellement cochées
        private List<string> GetSelectedCities()
        {
            return checkedListCities.CheckedItems
                .Cast<object>()
                .Select(o => o.ToString() ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        // Remplit/actualise les ComboBox d'années pour les villes cochées
        private void RefreshYearsForSelection(bool preserveSelection = false)
        {
            _isUpdatingYears = true;

            int? prevFrom = comboYearFrom.SelectedItem as int?;
            int? prevTo = comboYearTo.SelectedItem as int?;

            var selCities = GetSelectedCities();

            IEnumerable<int> yearsQuery = _records
                .Where(r => selCities.Count == 0 ||
                            selCities.Contains(r.City, StringComparer.OrdinalIgnoreCase))
                .Select(r => r.Year);

            var years = yearsQuery.Distinct().OrderBy(y => y).ToList();

            if (years.Count == 0)
            {
                comboYearFrom.DataSource = null;
                comboYearTo.DataSource = null;
                _isUpdatingYears = false;
                return;
            }

            comboYearFrom.DataSource = years.ToList(); // copies séparées
            comboYearTo.DataSource = years.ToList();

            int newFrom = years.First();
            int newTo = years.Last();

            if (preserveSelection && prevFrom.HasValue && years.Contains(prevFrom.Value))
                newFrom = prevFrom.Value;
            if (preserveSelection && prevTo.HasValue && years.Contains(prevTo.Value))
                newTo = prevTo.Value;

            if (newFrom > newTo) (newFrom, newTo) = (newTo, newFrom);

            comboYearFrom.SelectedItem = newFrom;
            comboYearTo.SelectedItem = newTo;

            _isUpdatingYears = false;
        }

        // Lit la sélection et trace
        private void PlotCurrentSelection()
        {
            var selCities = GetSelectedCities();
            if (selCities.Count == 0)
            {
                formsPlot1.Plot.Clear();
                formsPlot1.Plot.Title("Sélectionne au moins une ville");
                formsPlot1.Refresh();
                return;
            }

            if (comboYearFrom.SelectedItem is not int yearFrom) return;
            if (comboYearTo.SelectedItem is not int yearTo) return;

            // Sécurité: force yearFrom <= yearTo
            if (yearFrom > yearTo)
            {
                (yearFrom, yearTo) = (yearTo, yearFrom);
                _isUpdatingYears = true;
                comboYearFrom.SelectedItem = yearFrom;
                comboYearTo.SelectedItem = yearTo;
                _isUpdatingYears = false;
            }

            PlotCitiesMonthlyTimeline(selCities, yearFrom, yearTo);
        }

        /// <summary>
        /// Trace une timeline mensuelle continue de yearFrom à yearTo (12 points/an),
        /// pour chaque ville cochée (une courbe par ville).
        /// </summary>
        private void PlotCitiesMonthlyTimeline(List<string> cities, int yearFrom, int yearTo)
        {
            formsPlot1.Plot.Clear();

            int totalMonths = (yearTo - yearFrom + 1) * 12;

            //  Ticks MOIS (répétés) avec décimation automatique 
            int step = totalMonths <= 36 ? 1 :
                       totalMonths <= 120 ? 2 :
                       totalMonths <= 240 ? 3 : 6;

            var monthTickPos = new List<double>();
            var monthTickLab = new List<string>();
            for (int i = 0; i < totalMonths; i += step)
            {
                monthTickPos.Add(i);
                monthTickLab.Add(monthLabels[i % 12]);
            }

            formsPlot1.Plot.Axes.Bottom.TickGenerator =
                new ScottPlot.TickGenerators.NumericManual(monthTickPos.ToArray(), monthTickLab.ToArray());

            //  Marqueurs annuels (traits verticaux à chaque janvier) 
            for (int y = 0; y <= (yearTo - yearFrom); y++)
            {
                double januaryX = y * 12; // position de janvier
                var vline = formsPlot1.Plot.Add.VerticalLine(januaryX);
                vline.LineWidth = 1;
                vline.LinePattern = LinePattern.Dashed;
            }

            //  Une courbe par ville 
            double[] x = Enumerable.Range(0, totalMonths).Select(i => (double)i).ToArray();

            foreach (var city in cities)
            {
                var rowsCity = _records
                    .Where(r => string.Equals(r.City, city, StringComparison.OrdinalIgnoreCase)
                                && r.Year >= yearFrom && r.Year <= yearTo)
                    .ToList();

                if (rowsCity.Count == 0)
                    continue;

                // Moyenne par (Year, Month) pour la ville
                var meanByYearMonth = rowsCity
                    .GroupBy(r => (r.Year, r.Month))
                    .ToDictionary(
                        g => g.Key,
                        g => g.Average(x => x.AvgTemperature)
                    );

                var yValues = new double[totalMonths];
                int idx = 0;
                for (int y = yearFrom; y <= yearTo; y++)
                {
                    for (int m = 1; m <= 12; m++)
                    {
                        yValues[idx++] = meanByYearMonth.TryGetValue((y, m), out double avg)
                            ? avg
                            : double.NaN;
                    }
                }

                var line = formsPlot1.Plot.Add.Scatter(x, yValues);
                line.LegendText = city;
                line.LineWidth = 2;
            }

            // Titre
            string titleCities = cities.Count <= 3 ? string.Join(", ", cities) : $"{cities.Count} villes";
            formsPlot1.Plot.YLabel("Température moyenne (°C)");
            formsPlot1.Plot.Title($"Températures mensuelles – {titleCities} ({yearFrom}–{yearTo})");
            formsPlot1.Plot.Legend.IsVisible = true;

            formsPlot1.Plot.Axes.AutoScale();
            formsPlot1.Refresh();
        }


    }
}
