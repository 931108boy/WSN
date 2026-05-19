using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    class GammaDistribution
    {
        public static Random randObj1 = new Random();
        public static double GammaPDF(double x, int b, double a)
        {
            return Math.Pow(a, b) * Math.Pow(x, b - 1) * Math.Exp(-a * x) / Gamma(b);
        }
        public static double[] GammaPDF(double[] x, int b, double a)
        {
            double[] tempArray = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                tempArray[i] = GammaPDF(x[i], b, a);
            }
            return tempArray;
        }
        public static double NextGamma(int b, double a)
        {
            double temp = 0.0;
            for (int i = 0; i < b; i++)
            {
                temp += -Math.Log(randObj1.NextDouble()) / a;
            }
            return temp;
        }
        public static double[] NextGamma(int b, double a, int nLen)
        {
            double[] tempArr = new double[nLen];
            for (int i = 0; i < nLen; i++)
            {
                tempArr[i] = NextGamma(b, a);
            }
            return tempArr;
        }
        public static double GammaLn(double x)
        {
            if (x <= 0) throw
            new Exception("Input value must be > 0");
            double[] coef = new double[14]
            {57.1562356658629235,
            -59.5979603554754912,
            14.1360979747417471,
            -0.491913816097620199,
            0.339946499848118887E-4,
            0.465236289270485756E-4,
            -0.983744753048795646E-4,
            0.158088703224912494E-3,
            -0.210264441724104883E-3,
            0.217439618115212643E-3,
            -0.164318106536763890E-3,
            0.844182239838527433E-4,
            -0.261908384015814087E-4,
            0.368991826595316234E-5};
            double denominator = x;
            double series = 0.999999999999997092;
            double temp = x + 5.24218750000000000;
            temp = (x + 0.5) * Math.Log(temp) - temp;
            for (int j = 0; j < 14; j++)
                series += coef[j] / ++denominator;
            return (temp + Math.Log(2.5066282746310005 * series / x));
        }
        public static double Gamma(double x)
        {
            if (x <= 0) throw
            new Exception("Input value must be > 0");
            return Math.Exp(GammaLn(x));
        }
    }
}
