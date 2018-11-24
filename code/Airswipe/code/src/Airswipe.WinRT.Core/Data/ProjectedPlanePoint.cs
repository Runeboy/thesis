using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.Data
{
    public interface ProjectedPlanePoint<TSource> : PlanePoint, PlaneProjection<TSource> where TSource : SpatialPoint
    {
        //PlanePoint Delta { get; }
    }
}
