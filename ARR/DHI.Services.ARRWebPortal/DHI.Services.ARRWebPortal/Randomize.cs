using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public static class Randomize
    {
        public static List<double> GetRandomValues(double mean, double stddev, int count)
        {
            List<double> result = new List<double>();
            
            Random r = new Random();
            
            for (int i = 0; i <= count; i++)
            {
                result.Add(SampleGaussian(r, mean, stddev));
            }

            return result;
        }

        public static double SampleGaussian(Random random, double mean, double stddev)
        {
            // The method requires sampling from a uniform random of (0,1]
            // but Random.NextDouble() returns a sample of [0,1).
            double x1 = 1 - random.NextDouble();
            double x2 = 1 - random.NextDouble();

            double y1 = System.Math.Sqrt(-2.0 * System.Math.Log(x1)) * System.Math.Cos(2.0 * System.Math.PI * x2);
            return y1 * stddev + mean;

            //alternative method
            //(1/(Math.Sqrt(2*Math.Pi)))*(Math.Exp(-((x/2)^2)))

        }

        public static List<double> GetRandomValues(double stddev, int count)
        {
            List<double> result = new List<double>();

            Random r = new Random();

            for (int i = 0; i <= count; i++)
            {
                result.Add(SampleGaussian(r, stddev));
            }

            return result;
        }

        public static double SampleGaussian(Random random, double stddev)
        {
            // The method requires sampling from a uniform random of (0,1]
            // but Random.NextDouble() returns a sample of [0,1).
            double x1 = 1 - random.NextDouble();
            double x2 = 1 - random.NextDouble();

            double y1 = System.Math.Sqrt(-2.0 * System.Math.Log(x1)) * System.Math.Cos(2.0 * System.Math.PI * x2);
            return y1 * stddev;

            //alternative method
            //(1/(Math.Sqrt(2*Math.Pi)))*(Math.Exp(-((x/2)^2)))

        }
    }
}
