using CsvHelper;
using CsvHelper.Configuration;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PTL
{
    public partial class Form1 : Form
    {
        // Libellés de mois (répétés sur toute la timeline)
        string[] monthLabels = { "Jan", "Fév", "Mar", "Avr", "Mai", "Juin",
                                 "Jui", "Aou", "Sep", "Oct", "Nov", "Déc" };

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

        // Limiter le nombre de villes cochées
        private const int MAX_CITIES = 5;

        public Form1()
        {
            InitializeComponent();

            // Import CSV
            this.Load += formsPlot1_Load;

            // === CheckedListBox pour villes ===
            checkedListCities.CheckOnClick = true;
            checkedListCities.ItemCheck += checkedListCities_ItemCheck;

            // Combos années
            comboYearFrom.DropDownStyle = ComboBoxStyle.DropDownList;
            comboYearTo.DropDownStyle = ComboBoxStyle.DropDownList;
            comboYearFrom.SelectedIndexChanged += Years_SelectedIndexChanged;
            comboYearTo.SelectedIndexChanged += Years_SelectedIndexChanged;

            // Sélecteur mois/année
            comboGranularity.DropDownStyle = ComboBoxStyle.DropDownList;
            comboGranularity.Items.AddRange(new[] { "Mois", "Année" });
            comboGranularity.SelectedIndex = 0; // par défaut: Mois
            comboGranularity.SelectedIndexChanged += comboGranularity_SelectedIndexChanged;
        }

        private void formsPlot1_Load(object? sender, EventArgs e)
        {
            checkedListCities.Items.Clear();
            formsPlot1.Plot.Clear();
            formsPlot1.Plot.Title("Clique sur « Importer CSV… » pour commencer");
            formsPlot1.Refresh();
        }

        // Coche/décoche une ville 
        private void checkedListCities_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            // Limite de villes cochées
            if (MAX_CITIES > 0 && e.NewValue == CheckState.Checked && checkedListCities.CheckedItems.Count >= MAX_CITIES)
            {
                e.NewValue = CheckState.Unchecked;
                MessageBox.Show($"Tu peux sélectionner au maximum {MAX_CITIES} villes.",
                    "Limite atteinte", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Recalculer années + re-tracer une fois l'état mis à jour
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

            int? prevFrom = comboYearFrom.SelectedItem is int a ? a : (int?)null;
            int? prevTo = comboYearTo.SelectedItem is int b ? b : (int?)null;

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

            // Force yearFrom <= yearTo
            if (yearFrom > yearTo)
            {
                (yearFrom, yearTo) = (yearTo, yearFrom);
                _isUpdatingYears = true;
                comboYearFrom.SelectedItem = yearFrom;
                comboYearTo.SelectedItem = yearTo;
                _isUpdatingYears = false;
            }

            // Choix de la granularité (Mois / Année)
            bool byYear = string.Equals(
                comboGranularity.SelectedItem?.ToString(),
                "Année",
                StringComparison.OrdinalIgnoreCase
            );

            if (byYear)
                PlotCitiesYearlyTimeline(selCities, yearFrom, yearTo);
            else
                PlotCitiesMonthlyTimeline(selCities, yearFrom, yearTo);
        }

        private void comboGranularity_SelectedIndexChanged(object sender, EventArgs e)
        {
            PlotCurrentSelection();
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

            // Repères annuels (traits verticaux à chaque janvier)
            for (int y = 0; y <= (yearTo - yearFrom); y++)
            {
                double januaryX = y * 12; // position de janvier
                var vline = formsPlot1.Plot.Add.VerticalLine(januaryX);
                vline.LineWidth = 1;
                vline.LinePattern = LinePattern.Dashed;
            }

            // Une courbe par ville 
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

            // Style axes/labels
            formsPlot1.Plot.YLabel("Température moyenne (°C)");
            formsPlot1.Plot.Axes.Bottom.Label.Text = "Mois";
            formsPlot1.Plot.Axes.Color(new("#FFD700"));

            // Titre et légende
            string titleCities = cities.Count <= 3 ? string.Join(", ", cities) : $"{cities.Count} villes";
            formsPlot1.Plot.Title($"Températures mensuelles – {titleCities} ({yearFrom}–{yearTo})");
            formsPlot1.Plot.Legend.IsVisible = true;

            formsPlot1.Plot.Axes.AutoScale();
            formsPlot1.Refresh();
        }

        /// <summary>
        /// Trace une timeline ANNUELLE de yearFrom à yearTo,
        /// pour chaque ville cochée (une courbe par ville).
        /// 1 point par année = moyenne des 12 moyennes mensuelles de l'année.
        /// </summary>
        private void PlotCitiesYearlyTimeline(List<string> cities, int yearFrom, int yearTo)
        {
            formsPlot1.Plot.Clear();

            // Abscisses: une position par année (0..N-1), labels = années
            int totalYears = (yearTo - yearFrom + 1);
            double[] x = Enumerable.Range(0, totalYears).Select(i => (double)i).ToArray();
            string[] yearLabels = Enumerable.Range(yearFrom, totalYears).Select(y => y.ToString()).ToArray();

            // Ticks d'années avec décimation si nécessaire
            int step = totalYears <= 15 ? 1 :
                       totalYears <= 30 ? 2 :
                       totalYears <= 60 ? 3 : 5;

            var tickPos = new List<double>();
            var tickLab = new List<string>();
            for (int i = 0; i < totalYears; i += step)
            {
                tickPos.Add(i);
                tickLab.Add(yearLabels[i]);
            }

            formsPlot1.Plot.Axes.Bottom.TickGenerator =
                new ScottPlot.TickGenerators.NumericManual(tickPos.ToArray(), tickLab.ToArray());

            // Une courbe par ville 
            foreach (var city in cities)
            {
                var rowsCity = _records
                    .Where(r => string.Equals(r.City, city, StringComparison.OrdinalIgnoreCase)
                                && r.Year >= yearFrom && r.Year <= yearTo)
                    .ToList();

                if (rowsCity.Count == 0)
                    continue;

                // 1) moyenne par (Year, Month)
                var monthlyMeans = rowsCity
                    .GroupBy(r => (r.Year, r.Month))
                    .Select(g => new { Year = g.Key.Year, Mean = g.Average(x => x.AvgTemperature) })
                    .ToList();

                // 2) moyenne des 12 mois par année
                var meanByYear = monthlyMeans
                    .GroupBy(m => m.Year)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Average(m => m.Mean)
                    );

                var yValues = new double[totalYears];
                for (int i = 0; i < totalYears; i++)
                {
                    int year = yearFrom + i;
                    yValues[i] = meanByYear.TryGetValue(year, out double avg)
                        ? avg
                        : double.NaN;
                }

                var line = formsPlot1.Plot.Add.Scatter(x, yValues);
                line.LegendText = city + " (annuel)";
                line.LineWidth = 2;
            }

            // Repères annuels
            for (int i = 0; i < totalYears; i++)
            {
                var vline = formsPlot1.Plot.Add.VerticalLine(i);
                vline.LineWidth = 1;
                vline.LinePattern = LinePattern.Dashed;
            }

            // Style axes/labels 
            formsPlot1.Plot.YLabel("Température moyenne (°C)");
            formsPlot1.Plot.Axes.Bottom.Label.Text = "Années";
            formsPlot1.Plot.Axes.Color(new("#FFD700"));

            // Titre et légende
            string titleCities = cities.Count <= 3 ? string.Join(", ", cities) : $"{cities.Count} villes";
            formsPlot1.Plot.Title($"Températures annuelles – {titleCities} ({yearFrom}–{yearTo})");
            formsPlot1.Plot.Legend.IsVisible = true;

            formsPlot1.Plot.Axes.AutoScale();
            formsPlot1.Refresh();
        }

        private void btnImportCsv_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Choisir un fichier CSV",
                Filter = "Fichiers CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
                Multiselect = false
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    LoadCsv(ofd.FileName);
                    MessageBox.Show($"Import terminé : {_records.Count} lignes.", "CSV importé",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Échec de l'import : {ex.Message}", "Erreur",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadCsv(string path)
        {
            // Lit la 1re ligne pour deviner le séparateur
            char delimiter = ',';
            using (var srDetect = new StreamReader(path, Encoding.UTF8, true))
            {
                string? firstLine = srDetect.ReadLine();
                delimiter = GuessDelimiter(firstLine);
            }

            // Config CsvHelper
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter.ToString(),
                HasHeaderRecord = true,
                PrepareHeaderForMatch = args => args.Header.Trim(),
                MissingFieldFound = null,   // ignore champs manquants
                BadDataFound = null,        // ignore lignes mal formées
                DetectColumnCountChanges = true
            };

            // Lecture et mapping vers record Temperature
            using var reader = new StreamReader(path, Encoding.UTF8, true);
            using var csv = new CsvReader(reader, cfg);

            _records = csv.GetRecords<Temperature>().ToList();

            // Mets à jour l’UI
            RefreshUiAfterLoad();
        }

        private static char GuessDelimiter(string? firstLine)
        {
            if (string.IsNullOrEmpty(firstLine)) return ',';
            int commas = firstLine.Count(c => c == ',');
            int semis = firstLine.Count(c => c == ';');
            int tabs = firstLine.Count(c => c == '\t');

            if (semis >= commas && semis >= tabs) return ';';
            if (tabs >= commas && tabs >= semis) return '\t';
            return ',';
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboYearTo_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void RefreshUiAfterLoad()
        {
            // Recharger la liste des villes dans la CheckedListBox
            var cities = _records
                .Select(r => r.City)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            checkedListCities.BeginUpdate();
            checkedListCities.Items.Clear();
            foreach (var c in cities)
                checkedListCities.Items.Add(c, false);

            // Sélection initiale : cocher la première ville si dispo
            if (checkedListCities.Items.Count > 0)
                checkedListCities.SetItemChecked(0, true);
            checkedListCities.EndUpdate();

            // Met à jour la plage d'années et trace
            RefreshYearsForSelection();
            PlotCurrentSelection();
        }
    }
}
