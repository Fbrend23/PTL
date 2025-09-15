using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using static System.Console;
using System.Text.RegularExpressions;

namespace PTL
{
    public partial class Form1 : Form
    {
        string[] monthLabels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                         "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        // Représente une ligne du CSV (en-têtes doivent correspondre)
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

        public Form1()
        {
            InitializeComponent();
            // On trace au chargement du formulaire
            Load += formsPlot1_Load;
        }

        private void formsPlot1_Load(object? sender, EventArgs e)
        {
           
            var path = @"C:\Users\pl76tup\Desktop\PTL\PTL\Données\city_temperature.csv";


                using var reader = new StreamReader(path);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.TypeConverterCache.AddConverter<double>(new CelsiusConverter());
                var records = csv.GetRecords<Temperature>().ToList();

                // Choisis la ville à afficher
                var selectedCityName = records.First().City;
                var city = records.First(r => r.City == selectedCityName);

                // Construit les Y (températures sur 12 mois)
                double y = 
                ;

                // Axe X
                double[] x = Enumerable.Range(1, 12).Select(i => (double)i).ToArray();
                formsPlot1.Plot.Axes.Bottom.TickGenerator =
                 new ScottPlot.TickGenerators.NumericManual(
                Enumerable.Range(1, 12).Select(i => (double)i).ToArray(),
                labels: monthLabels
                 );
            // Reset du plot si on recharge
            formsPlot1.Plot.Clear();

                // Ajout de la série
                var scat = formsPlot1.Plot.Add.Scatter(x, y);
                scat.LegendText = city.City;
                scat.LineWidth = 2;

                // Libellés d’axes
                formsPlot1.Plot.YLabel("Température moyenne (°C)");
                formsPlot1.Plot.Title($"Températures moyennes – {city.City}");

                // Légende visible
                formsPlot1.Plot.Legend.IsVisible = true;

                // Petites marges autour du tracé
                formsPlot1.Plot.Axes.AutoScale();

                // Rafraîchit l’affichage
                formsPlot1.Refresh();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        public class CelsiusConverter : CsvHelper.TypeConversion.DoubleConverter
        {
            public override object? ConvertFromString(string? text, CsvHelper.IReaderRow row, CsvHelper.Configuration.MemberMapData memberMapData)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return double.NaN;

                // Supprime tout ce qui est entre parenthèses (et les parenthèses elles-mêmes)
                string cleaned = Regex.Replace(text, @"\(.*?\)", "").Trim();

                // Exemple: "11.2 (52.2)" -> "11.2"
                return double.Parse(cleaned, CultureInfo.InvariantCulture);
            }
        }

    }
}
