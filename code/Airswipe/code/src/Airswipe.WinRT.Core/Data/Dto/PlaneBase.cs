using System;
using System.Collections.Generic;

namespace Airswipe.WinRT.Core.Data.Dto
{
    public abstract class PlaneBase<TPoint> where TPoint : PointLocation<TPoint>
    {
        protected static void ErrorIfVectorsNotOthogonal(IEnumerable<TPoint> orthogonals)
        {
            foreach (var o1 in orthogonals)
                foreach (var o2 in orthogonals)
                    if (o1 != null && o2 != null && !o1.Equals(o2) && !o1.IsOrthogonalToApprox(o2)) // -e13
                        throw new Exception("One or more vectors are not orthogonal.");
        }


    }
}
