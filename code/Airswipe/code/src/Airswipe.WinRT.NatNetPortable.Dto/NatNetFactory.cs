using Airswipe.WinRT.Core.Data.MotrionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.NatNetPortable
{
    public static class NatNetFactory
    {
        #region Methods

        public IServerDescription CreateServerDescription() { return new ServerDescription(); }

        public IFrameOfMocapData CreateFrame() { return new FrameOfMocapData(); }

        #endregion
    }
}
