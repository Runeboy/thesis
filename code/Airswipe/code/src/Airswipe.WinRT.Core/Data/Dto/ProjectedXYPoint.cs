using System;
using Airswipe.WinRT.Core.MotionTracking;

namespace Airswipe.WinRT.Core.Data.Dto
{
    public class ProjectedXYPoint : XYPoint, ProjectedPlanePoint<SpatialPoint>
    {
        #region Properties

        public double ProjectionDistance { get; set; }

        public SpatialPoint Source { get; set; }

        public PlanePoint Delta { get; set; }

        public ProjectionMode Mode { get; set; }

        //public PlanePoint Delta { get; }

        #endregion
    }
}
