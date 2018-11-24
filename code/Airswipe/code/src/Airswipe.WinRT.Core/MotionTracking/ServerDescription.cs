using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public interface ServerDescription
    {
        #region Properties

        int[] HostAppVersion { get; }

        int[] NatNetVersion { get; }

        string HostApp { get; }

        #endregion
    }
}
