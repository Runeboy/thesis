using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace Airswipe.WinRT.NatNetPortable
{
    public class NatNetMotionBroadcastDetector : MotionBroadcastDetector
    {
        #region Fields

        private static readonly ILogger log = new TypeLogger<NatNetMotionBroadcastDetector>();

        private const int NATNET_BROADCAST_PORT = 1511;
        //private const string NATNET_BROADCAST_GROUP_ADDRESS = "255.255.255.255"
        private const string NATNET_BROADCAST_GROUP_ADDRESS = "239.255.42.99";

        private DatagramSocket socket;

        public event MotionBroadcastDetected MotionBroadcastDetected; 

        #endregion
        #region Constructors

        public NatNetMotionBroadcastDetector()
        {
            log.Info("construct");

            socket = new DatagramSocket();

            NotifyBroadcasterExistenceOnMotionBroadcastReceived();
            JoinNatNetMulticastGroup();
        }
        
        private async void JoinNatNetMulticastGroup()
        {
            await socket.BindServiceNameAsync(NATNET_BROADCAST_PORT.ToString());

            socket.JoinMulticastGroup(new HostName(NATNET_BROADCAST_GROUP_ADDRESS));
        }

        #endregion
        #region Methods

        private void NotifyBroadcasterExistenceOnMotionBroadcastReceived()
        {
            socket.MessageReceived += (sender, args) =>
            {
                DetectedMotionBroadcastHost = args.RemoteAddress;

                log.Info("Detected motion broadcaster: " + DetectedMotionBroadcastHost);
                //Debug.WriteLine("******************************");

                if (MotionBroadcastDetected != null)
                    MotionBroadcastDetected(DetectedMotionBroadcastHost);

                socket.Dispose();
            };
        }

        #endregion
        #region Properties

        public HostName DetectedMotionBroadcastHost {  get; private set; }

        #endregion
    }
}
