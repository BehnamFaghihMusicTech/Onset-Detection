using System;
using System.Collections.Generic;


namespace OnsetDetector
{
    public class MathFunctions
    {
        public static double StandardDeviation(List<double> values, double avg, bool sample=false)
        {
            double sd = Math.Sqrt(Variance(values, avg, sample));
            return sd;
        }

        public static double Variance(List<double> values, double avg, bool sample = false)
        {
            double var = 0;
            for (int i = 0; i < values.Count; i++)
            {
                var += Math.Pow((values[i] - avg), 2);
            }
            if (sample)
                var /= (values.Count - 1);
            else
                var /= (values.Count);
            return var;
        }
    }
}
