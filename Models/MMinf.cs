using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

namespace Models
{
    public class MMinf
    {
        public MMinf() { }

        static double Factorial(double x)
        {
            return (x < 0) ? -1 : (x == 0) ? 1 : (x < 20) ? x * Factorial(x - 1) : Math.Sqrt(2 * Math.PI * x) * (Math.Pow(x / Math.E, x));
        }
        public static double CalcK_Avg(double lambda, double mu)
        {
            return lambda / mu;
        }

        public static double CalcWs_Avg(double lambda, double mu)
        {
            return 1 / mu;
        }

        public static void CalcPk(double lambda, double mu, ListView list, WinRTXamlToolkit.Controls.DataVisualization.Charting.Chart lineChart)
        {
            lineChart.Title = "P(k)";
            List<Point> chartList = new List<Point>();
            double ro = lambda / mu;
            for (int k = 0; k <= 10; k++)
            {
                list.Items.Add(k + ") " + Math.Pow(ro, k) * Math.Pow(Math.E, -ro) / Factorial(k));
                chartList.Add(new Point() { x_axis = k, y_axis = Math.Pow(ro, k) * Math.Pow(Math.E, -ro) / Factorial(k) });
            }
            (lineChart.Series[0] as AreaSeries).ItemsSource = chartList;
            (lineChart.Series[0] as AreaSeries).Title = "P(k)";
            (lineChart.Series[1] as AreaSeries).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            (lineChart.Series[1] as AreaSeries).Title = "";
        }
        public static string CortanaCalkPk(double lambda, double mu, int k)
        {
            double ro = lambda / mu;
            return (Math.Pow(ro, k) * Math.Pow(Math.E, -ro) / Factorial(k)).ToString();
        }
    }
}
