using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public interface MotionBroadcastDetector
    {
        #region Events 

        event MotionBroadcastDetected MotionBroadcastDetected;

        #endregion
        #region Properties

        HostName DetectedMotionBroadcastHost { get; }

        #endregion
    }
}
