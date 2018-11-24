using Airswipe.WinRT.Core.Data;
using Windows.Foundation;
//using Windows.Foundation;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public struct Calibration
    {
        public XYZPlane OffsceenPlane { get; set; }

        public Rect DisplayDimension { get; set; }
    }
}
