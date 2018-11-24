using System;

namespace Airswipe.WinRT.Core.Data
{
    public class Circle
    {
        public double Radius { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public bool Contains(double x, double y)
        {
            double distanceFromCenter = Math.Sqrt(
                Math.Pow(X - x, 2) + Math.Pow(Y - y, 2)
                );

            return distanceFromCenter <= Radius;
        } 

    }
}
