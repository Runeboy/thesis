using System.Collections.Generic;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public delegate void FrameBatchReady(List<FrameOfMocapData> data, SimplifiedMotionTrackerClient client);
}
