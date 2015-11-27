using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

namespace MathModels
{
    class MMV
    {
        public MMV() { }
        static double Factorial(double x)
        {
            return (x < 0) ? -1 : (x == 0) ? 1 : (x < 20) ? x * Factorial(x - 1) : Math.Sqrt(2 * Math.PI * x) * (Math.Pow(x / Math.E, x));
        }

        public static double CalcGamma_Avg(double lambda, double mu, int v)
        {
            return 1 / (mu * (v - lambda / mu));
        }

        public static double CalcJ_Avg(double lambda, double mu, int v)
        {
            return lambda * CalcGamma_Avg(lambda, mu, v);
        }

        public static void CalcPi(double lambda, double mu, int v, ListView list, WinRTXamlToolkit.Controls.DataVisualization.Charting.Chart lineChart)
        {
            List<View.Point> chartList = new List<View.Point>();
            double znam = 0;
            double ro = lambda / mu;
            for (int x = 0; x <= v - 1; x++)
            {
                znam += Math.Pow(ro, x) / Factorial(x);
            }
            znam += Math.Pow(ro, v) * v / (Factorial(v) * (v - ro));
            for (int i = 0; i <= v; i++)
            {
                list.Items.Add(i + ") " + Math.Pow(ro, i) / Factorial(i) / znam);
                chartList.Add(new View.Point() { x_axis = i, y_axis = Math.Pow(ro, i) / Factorial(i) / znam });
            }
            (lineChart.Series[0] as AreaSeries).ItemsSource = chartList;
            (lineChart.Series[0] as AreaSeries).Title = "P(i)";
        }

        public static void CalcWj(double lambda, double mu, int v, ListView list, WinRTXamlToolkit.Controls.DataVisualization.Charting.Chart lineChart)
        {
            List<View.Point> chartList2 = new List<View.Point>();
            double znam = 0;
            double ro = lambda / mu;
            for (int x = 0; x <= v - 1; x++)
            {
                znam += Math.Pow(ro, x) / Factorial(x);
            }
            znam += Math.Pow(ro, v) * v / (Factorial(v) * (v - ro));
            for (int j = 0; j <= v + 10; j++)
            {
                list.Items.Add(j + ") " + Math.Pow(ro, v) / Factorial(v) * Math.Pow(ro / v, j) / znam);
                chartList2.Add(new View.Point() { x_axis = j+v, y_axis = Math.Pow(ro, v) / Factorial(v) * Math.Pow(ro / v, j) / znam });
            }
            (lineChart.Series[1] as AreaSeries).ItemsSource = chartList2;
            (lineChart.Series[1] as AreaSeries).Title = "W(j)";
        }

        public static double CalcPt(double lambda, double mu, int v)
        {
            double znam = 0;
            double ro = lambda / mu;
            for (int x = 0; x <= v - 1; x++)
            {
                znam += Math.Pow(ro, x) / Factorial(x);
            }
            znam += Math.Pow(ro, v) * v / (Factorial(v) * (v - ro));
            return Math.Pow(ro, v) / Factorial(v) * v / (v-ro) / znam;
        }
    }
}
