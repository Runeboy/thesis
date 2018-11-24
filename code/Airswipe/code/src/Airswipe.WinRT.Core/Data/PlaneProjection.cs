using Airswipe.WinRT.Core.MotionTracking;

namespace Airswipe.WinRT.Core.Data
{
    public interface PlaneProjection<TPoint>// : 
    {
        double ProjectionDistance { get; }

        TPoint Source {get; }

        ProjectionMode Mode { get; }

    }
}
