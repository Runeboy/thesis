using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public interface MotionTrackerClient
    {
        #region Events

        event FrameReady OnFrameReady;

        #endregion
        #region Methods

        int Uninitialize();

        int Initialize(String localAddress, String serverAddress);

        int GetServerDescription(ServerDescription desc);
        //ServerDescription GetServerDescription();

        int[] NatNetVersion();

        int SendMessageAndWait(string message, out byte[] serverResponse, out int responseSize);

        List<DataDescriptor> GetDataDescriptions();

        bool GetDataDescriptions(out List<DataDescriptor> descriptions);

        FrameOfMocapData GetLastFrameOfData(bool processAsBroadcastFrame);

        #endregion
    }
}
