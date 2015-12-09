using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

namespace Models
{
    public class MM1
    {
        public MM1() { }

        public static double CalcK_Avg(double lambda, double mu)
        {
            return lambda / (mu - lambda);
        }

        public static double CalcWs_Avg(double lambda, double mu)
        {
            return 1 / (mu - lambda);
        }

        public static double CalcLq_Avg(double lambda, double mu)
        {
            return Math.Pow(lambda, 2) / (mu * (mu - lambda));
        }

        public static double CalcWq_Avg(double lambda, double mu)
        {
            return lambda / (mu * (mu - lambda));
        }
        public static void CalcPk(double lambda, double mu, ListView list, WinRTXamlToolkit.Controls.DataVisualization.Charting.Chart lineChart)
        {
            lineChart.Title = "P(k)";
            List<Point> chartList = new List<Point>();
            double ro = lambda / mu;
            for (int k = 0; k <= 10; k++)
            {
                list.Items.Add(k + ") " + (1 - ro) * Math.Pow(ro, k));
                chartList.Add(new Point() { x_axis = k, y_axis = (1 - ro) * Math.Pow(ro, k) });
            }
            (lineChart.Series[0] as AreaSeries).ItemsSource = chartList;
            (lineChart.Series[0] as AreaSeries).Title = "P(k)";
            (lineChart.Series[1] as AreaSeries).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            (lineChart.Series[1] as AreaSeries).Title = "";
        }
        public static string CortanaCalkPk(double lambda, double mu, int k)
        {
            double ro = lambda / mu;
            return ((1 - ro) * Math.Pow(ro, k)).ToString();
        }
    }
}
