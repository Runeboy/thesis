using Airswipe.WinRT.Core.Misc;
using System.Collections.Generic;
using System.Linq;

namespace Airswipe.WinRT.Core
{
    public class OutlierFilter
    {
        public double upperOuterFrence;
        public readonly double lowerOuterFence;

        public double upperInnerFrence;
        public readonly double lowerInnerFence;

        public readonly double interquartileRange;

        public readonly double lowerQuartile;
        public readonly double upperQuartile;

        public OutlierFilter(params double[] values) :this(values.ToList())
        {}

        public OutlierFilter(IEnumerable<double> values)
        {
            lowerQuartile = MathExpert.GetLowerQuartile(values);
            upperQuartile = MathExpert.GetUpperQuartile(values);

            interquartileRange = upperQuartile - lowerQuartile;

            lowerOuterFence = lowerQuartile - 3 * interquartileRange;
            upperOuterFrence = upperQuartile + 3 * interquartileRange;


            lowerInnerFence = lowerQuartile - 1.5 * interquartileRange;
            upperInnerFrence = upperQuartile + 1.5 * interquartileRange;
        }

        public bool IsExtremeOutlier(double value)
        {
            return (value < lowerOuterFence || value > upperOuterFrence);
        }

        public bool IsMildOutlier(double value)
        {
            return (value < lowerInnerFence || value > upperInnerFrence);
        }
    }

}
