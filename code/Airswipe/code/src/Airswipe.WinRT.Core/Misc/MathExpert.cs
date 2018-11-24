using System;
using System.Collections.Generic;
using System.Linq;

namespace Airswipe.WinRT.Core.Misc
{
    public class MathExpert
    {

        public static double EllipsePerimeteRamanujanrApprox(double a, double b)
        {
            return Math.PI * (3 * (a + b) - Math.Sqrt((3 * a + b) * (a + 3 * b)));
        }

        public static double GetCircleRadius(double circumference)
        {
            return circumference / (2.0 * Math.PI);
        }

        public static double GetUpperQuartile(IEnumerable<double> values)
        {
            return GetQuartile(values, 3);
        }

        public static double GetLowerQuartile(IEnumerable<double> values)
        {
            return GetQuartile(values, 1);
        }

        private static double GetQuartile(IEnumerable<double> values, int quartile)
        {
            if (quartile < 0 || quartile > 4)
                throw new ArgumentException("Bad quartile");


            var ascendingValues = values.OrderBy(v => v).ToList();

            //double median = GetMedian(values);

            double quartileValueNumber = (quartile / 4.0) * (values.Count() + 1);
            int quartileValueIndex = (int)quartileValueNumber - 1;

            double indexResidual = (quartileValueNumber - (int)quartileValueNumber);

            double valuesResidual = ascendingValues.ElementAt(quartileValueIndex + 1) - ascendingValues.ElementAt(quartileValueIndex);

            return ascendingValues.ElementAt(quartileValueIndex) +
                   indexResidual * valuesResidual;
        }

        public static double GetMedian(IEnumerable<double> values)
        {
            var ascendingValues = values.OrderBy(v => v).ToList();

            int size = ascendingValues.Count;

            double medianIndex = (size + 1) / 2.0 - 1;
            bool isIndexEven = medianIndex % 2 == 0;

            return isIndexEven ?
                ascendingValues.ElementAt((int)medianIndex) :
                (
                    ascendingValues.ElementAt((int)medianIndex) +
                    ascendingValues.ElementAt((int)medianIndex + 1)
                ) / 2.0;
        }

        public static double GetMean(IEnumerable<double> values)
        {
            return values.Sum() / values.Count();
        }

        public static double GetStandardDeviation(IEnumerable<double> values)
        {
            double mean = GetMean(values);
            int count = values.Count();
            IEnumerable<double> squaredMeanDiffValues = values.Select(v => Math.Pow(v - mean, 2));

            return Math.Sqrt(
                squaredMeanDiffValues.Sum() / count
                );
        }

        public static double GetStandardError(IEnumerable<double> values)
        {
            return GetStandardDeviation(values) / Math.Sqrt(values.Count());
        }

        public static double GetNinetyFiveConfMarginOfError(IEnumerable<double> values)
        {
            return 2 * GetStandardError(values);
        }

    }
}
