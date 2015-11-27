using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

namespace MathModels
{
    class MMVKN
    {
        public MMVKN() { }

        static double Factorial(double x)
        {
            return (x < 0) ? -1 : (x == 0) ? 1 : (x < 20) ? x * Factorial(x - 1) : Math.Sqrt(2 * Math.PI * x) * (Math.Pow(x / Math.E, x));
        }

        private static double Cnm(uint n, uint m)
        {
            if (m == n)
                return 1;
            if (m > n)
                return -1;
            return Factorial(n) / (Factorial(n - m) * Factorial(m));
        }

        public static void CalcPk(double a, double mu, int v, uint n, ListView list, WinRTXamlToolkit.Controls.DataVisualization.Charting.Chart lineChart)
        {
            lineChart.Title = "P(k)";
            List<View.Point> chartList = new List<View.Point>();
            double znam = 0;
            for (uint x = 0; x <= v; x++)
            {
                znam += Cnm(n, x) * Math.Pow(a / (1 - a), x);
            }
            for (uint k = 0; k <= v; k++)
            {
                list.Items.Add(k + ") " + Cnm(n, k) * Math.Pow(a / (1 - a), k) / znam);
                chartList.Add(new View.Point() { x_axis = k, y_axis = Cnm(n, k) * Math.Pow(a / (1 - a), k) / znam });
            }
            (lineChart.Series[0] as AreaSeries).ItemsSource = chartList;
            (lineChart.Series[0] as AreaSeries).Title = "P(k)";
            (lineChart.Series[1] as AreaSeries).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            (lineChart.Series[1] as AreaSeries).Title = "";
        }

        public static double CalcK_Avg(double a, double mu, int v, uint n)
        {
            double znam = 0;
            for (uint x = 0; x <= v; x++)
            {
                znam += Cnm(n, x) * Math.Pow(a / (1 - a), x);
            }
            double K = 0;
            for (uint x = 0; x <= v; x++)
            {
                K += x * Cnm(n, x) * Math.Pow(a / (1 - a), x) / znam;
            }
            return K;
        }

        public static double CalcT_Avg(double a, double mu, int v, uint n)
        {
            double znam = 0;
            for (uint x = 0; x <= v; x++)
            {
                znam += Cnm(n, x) * Math.Pow(a / (1 - a), x);
            }
            double K = 0;
            for (uint x = 0; x <= v; x++)
            {
                K += x * Cnm(n, x) * Math.Pow(a / (1 - a), x) / znam;
            }
            return K / mu;
        }

        public static double CalcPt(double a, uint v, uint n)
        {
            double znam = 0;
            for (uint x = 0; x <= v; x++)
            {
                znam += Cnm(n, x) * Math.Pow(a / (1 - a), x);
            }
            return Cnm(n, v) * Math.Pow(a / (1 - a), v) / znam;
        }

        public static double CalcPv(double a, uint v, uint n)
        {
            double znam = 0;
            for (uint x = 0; x <= v; x++)
            {
                znam += Cnm(n - 1, x) * Math.Pow(a / (1 - a), x);
            }
            return Cnm(n - 1, v) * Math.Pow(a / (1 - a), v) / znam;
        }
    }
}
