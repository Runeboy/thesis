using System;
using System.Collections.ObjectModel;
//using Windows.Foundation;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public interface SimplifiedMotionTrackerClient
    {
        #region Events

        event DisconnectComplete OnDisconnectComplete;
        event ConnectSucceeded OnConnectSucceeded;
        //event FrameBatchReady OnFrameBatchReady;

        event DataDescriptionReady OnDataDescriptionReady;
        event TrackedPointReadyHandler TrackedPointReady;

        #endregion
        #region Methods

        void GetDataDescriptions();

        void Connect(string strLocalIP, string strServerIP);

        void Disconnect();

        //       FrameOfMocapData GetLastFrameOfData(bool processAsBroadcastFrame);

        void SimulateFrameReceival(FrameOfMocapData frame);

        #endregion
        #region Properties

        double SmoothingBase { get; set; }

        double SmoothingDeltaMultiplier { get; set; }

        double SmoothingCutoffMilliSeconds { get; set; }


        string VersionString { get; }

        long FrameCount { get; }

        ObservableCollection<RigidBody> RigidBodies { get; }

        ObservableCollection<Skeleton> Skeletons { get; }

        ObservableCollection<MarkerSet> MarkerSets { get; }

        DateTime? ConnectTime { get; }

        //MotionClientStatistics Statistics { get; }

        OffscreenPoint LastPoint { get; }

        #endregion
    }
}
