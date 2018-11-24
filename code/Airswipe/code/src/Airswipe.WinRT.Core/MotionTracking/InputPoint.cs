using Airswipe.WinRT.Core.Data;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public class InputPoint<TOnscreenPoint, TOffscreenPoint> where TOnscreenPoint : PlanePoint where TOffscreenPoint : SpatialPoint
    {
        #region Properties

        public TOnscreenPoint Onscreen { get; set; }

        public TOffscreenPoint Offscreen { get; set; }

        #endregion
        #region Methods

        //public TimeSpan OffscreenCaptureLag
        //{
        //    get
        //    {
        //        var diff = Offscreen.CaptureTime - Onscreen.CaptureTime;
        //        if (diff.TotalMilliseconds == 0)
        //            throw new Exception("Offscreen capture lag was zero, which is an unlikely scenario.");

        //        return diff;
        //    }
        //}

        #endregion
    }
}
