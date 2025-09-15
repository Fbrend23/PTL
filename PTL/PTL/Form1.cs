using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using static System.Console;

namespace PTL
{
    public partial class Form1 : Form
    {
        // Représente une ligne du CSV (en-têtes doivent correspondre)
        public record Temperature
        {
            public string City { get; init; } = "";
            public double Jan { get; init; }
            public double Feb { get; init; }
            public double Mar { get; init; }
            public double Apr { get; init; }
            public double May { get; init; }
            public double Jun { get; init; }
            public double Jul { get; init; }
            public double Aug { get; init; }
            public double Sep { get; init; }
            public double Oct { get; init; }
            public double Nov { get; init; }
            public double Dec { get; init; }
        }

        public Form1()
        {
            InitializeComponent();
            // On trace au chargement du formulaire
            Load += formsPlot1_Load;
        }

        private void formsPlot1_Load(object? sender, EventArgs e)
        {
            // ⚠️ Utilise Path.Combine si possible (ici c’est un exemple simple)
            var path = @"C:\Users\pl76tup\Desktop\PTL\PTL\Données\Average Temperature of Cities.csv";
            if (!File.Exists(path))
            {
                MessageBox.Show($"Fichier introuvable:\n{path}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Configuration CsvHelper un peu plus tolérante
                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim,
                    MissingFieldFound = null, // ignore colonne manquante
                    BadDataFound = null       // ignore lignes “bizarres”
                };

                using var reader = new StreamReader(path);
                using var csv = new CsvReader(reader, csvConfig);
                var records = csv.GetRecords<Temperature>().ToList();

                if (records.Count == 0)
                {
                    MessageBox.Show("Le CSV ne contient aucune donnée.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Choisis la ville à afficher (ex: la première, ou une ville précise)
                // var city = records.First(); // première ville du fichier
                var selectedCityName = records.First().City; // ou "Zurich", "Tokyo", etc.
                var city = records.First(r => r.City == selectedCityName);

                // Construit les Y (températures sur 12 mois)
                double[] y =
                {
                    city.Jan, city.Feb, city.Mar, city.Apr, city.May, city.Jun,
                    city.Jul, city.Aug, city.Sep, city.Oct, city.Nov, city.Dec
                };

                // Axe X = 1..12
                double[] x = Enumerable.Range(1, 12).Select(i => (double)i).ToArray();

                // Reset du plot si on recharge
                formsPlot1.Plot.Clear();

                // Ajout de la série
                var scat = formsPlot1.Plot.Add.Scatter(x, y);
                scat.Label = city.City;    // pour la légende
                scat.LineWidth = 2;

                // Libellés d’axes + titre simple
                formsPlot1.Plot.XLabel("Mois (1 = Janvier … 12 = Décembre)");
                formsPlot1.Plot.YLabel("Température moyenne (°C)");
                formsPlot1.Plot.Title($"Températures moyennes – {city.City}");

                // Légende visible
                formsPlot1.Plot.Legend.IsVisible = true;

                // Petites marges autour du tracé
                formsPlot1.Plot.Axes.AutoScale();

                // Rafraîchit l’affichage
                formsPlot1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du CSV:\n{ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // (laisse vide si inutilisé)
        }
    }
}
