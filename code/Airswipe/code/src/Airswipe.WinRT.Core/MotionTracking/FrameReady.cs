using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public delegate void FrameReady(FrameOfMocapData data, MotionTrackerClient client);
}
