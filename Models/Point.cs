using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Point
    {
        public double x_axis { get; set; }
        public double y_axis { get; set; }
        public static string GetModelByNumber(string modelnumber)
        {
            switch (modelnumber)
            {
                case "one":
                case "1":
                case "first":
                    return "M|M|1";
                case "two":
                case "2":
                case "second":
                    return "M|M|∞";
                case "three":
                case "3":
                case "third":
                    return "M|M|V";
                case "four":
                case "4":
                case "fourth":
                    return "M|M|V|K";
                case "five":
                case "5":
                case "fifth":
                    return "M|M|V|K|N";
                default:
                    //return "Error page";
                    return "M|M|V";
            }
        }
        public static int GetNumberByModel(string modelname)
        {
            switch (modelname)
            {
                case "M|M|1":
                    return 1;
                case "M|M|∞":
                    return 2;
                case "M|M|V":
                    return 3;
                case "M|M|V|K":
                    return 4;
                case "M|M|V|K|N":
                    return 5;
                default:
                    return 0;
            }
        }
    }
}
