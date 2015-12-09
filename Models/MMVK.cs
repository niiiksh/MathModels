using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

namespace Models
{
    public class MMVK
    {
        public MMVK() { }

        static double Factorial(double x)
        {
            return (x < 0) ? -1 : (x == 0) ? 1 : (x < 20) ? x * Factorial(x - 1) : Math.Sqrt(2 * Math.PI * x) * (Math.Pow(x / Math.E, x));
        }

        public static void CalcPk(double lambda, double mu, int v, ListView list, WinRTXamlToolkit.Controls.DataVisualization.Charting.Chart lineChart)
        {
            lineChart.Title = "P(k)";
            List<Point> chartList = new List<Point>();
            double znam = 0;
            double ro = lambda / mu;
            for (int x = 0; x <= v; x++)
            {
                znam += Math.Pow(ro, x) / Factorial(x);
            }
            for (int k = 0; k <= v; k++)
            {
                list.Items.Add(k + ") " + Math.Pow(ro, k) / Factorial(k) / znam);
                chartList.Add(new Point() { x_axis = k, y_axis = Math.Pow(ro, k) / Factorial(k) / znam });
            }
            (lineChart.Series[0] as AreaSeries).ItemsSource = chartList;
            (lineChart.Series[0] as AreaSeries).Title = "P(k)";
            (lineChart.Series[1] as AreaSeries).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            (lineChart.Series[1] as AreaSeries).Title = "";
        }
        public static string CortanaCalkPk(double lambda, double mu, int v, int k)
        {
            double znam = 0;
            double ro = lambda / mu;
            for (int x = 0; x <= v; x++)
            {
                znam += Math.Pow(ro, x) / Factorial(x);
            }
            return (Math.Pow(ro, k) / Factorial(k) / znam).ToString();
        }
        public static double CalcPv(double lambda, double mu, int v)
        {
            double znam = 0;
            double ro = lambda / mu;
            for (int x = 0; x <= v; x++)
            {
                znam += Math.Pow(ro, x) / Factorial(x);
            }
            return Math.Pow(ro, v) / Factorial(v) / znam;
        }
    }
}
