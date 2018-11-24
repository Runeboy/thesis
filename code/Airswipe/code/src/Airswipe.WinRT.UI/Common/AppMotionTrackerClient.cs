using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.NatNetPortable;
using Airswipe.WinRT.Core;
using Airswipe.WinRT.Kinect;
using System;

namespace Airswipe.WinRT.UI.Common
{
    public class AppMotionTrackerClient  //SimplifiedNatNetPortableMotionTrackerClient
    {
        #region Fields

        private ILogger log = new TypeLogger<AppMotionTrackerClient>();

        private static SimplifiedMotionTrackerClient instance; // AppMotionTrackerClient instance;

        private static MotionClientStatistics clientStats;

        private static ConnectionType defaultConnectionType;

        #endregion
        #region Constructors

        private AppMotionTrackerClient(ConnectionType connectionType)
        //: base(connectionType, AppSettings.PointerMarkerID)
        {
            defaultConnectionType = connectionType;
        }

        #endregion
        #region Properties

        public static SimplifiedMotionTrackerClient Instance // AppMotionTrackerClient Instance
        {
            get
            {
                if (instance == null)
                    InitializeInstance(defaultConnectionType);

                return instance;
            }
            private set
            {
                if (instance != null)
                    instance.Disconnect();

                instance = value;
            }
        }

        private static void InitializeInstance(ConnectionType connectionType)
        {
            instance = CreateInstance(connectionType);
        }

        private static SimplifiedMotionTrackerClient CreateInstance(ConnectionType connectionType)
        {
            switch(AppSettings.TrackerType)
            {
                case TrackerTypes.Kinect:
                    return new SimplifiedKinectClient(AppSettings.KinectJointTypePointer, AppSettings.IS_KINECT_SMOOTHING_ENABLED, AppSettings.SmoothingBase, AppSettings.SmoothingCutoffMilliSeconds, AppSettings.SmoothingDeltaMultiplier);
                case TrackerTypes.NatNet:
                    return new SimplifiedNatNetPortableMotionTrackerClient(
                        connectionType,
                        AppSettings.PointerMarkerID,
                        AppSettings.SmoothingBase
                        );
            }

            throw new NotImplementedException("No tracker for given type.");
        }

        public static ConnectionType InstanceConnectionType
        {
            set { InitializeInstance(value); }
        }

        public static MotionClientStatistics Statistics
        {
            get
            {
                if (clientStats == null)
                    clientStats = new MotionClientStatistics(Instance, AppSettings.CLIENT_STAT_UPDATE_INTERVAL);

                return clientStats;
            }
        }

        #endregion
    }
}
