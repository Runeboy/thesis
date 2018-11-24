using System.Collections.Generic;

namespace Airswipe.WinRT.Core.Data
{
    public interface PointLocation<T> : PointComponents
    {
        double DistanceFrom(T p);

        T Subtract(T p);

        T Add(T p);

        double Dot(T p);

        //bool IsOrthogonalToApprox(T p);
        bool IsOrthogonalToApprox(PointComponents p);


        T Normalize();

        T Multiply(double f);
    }
}
