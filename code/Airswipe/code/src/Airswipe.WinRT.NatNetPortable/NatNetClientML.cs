using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Airswipe.WinRT.Core.MotionTracking;

namespace Airswipe.WinRT.NatNetPortable
{
    public class NatNetClientML : MotionTrackerClient //, NatNetPortableTrackerClient
    {
        #region Fields

        private NatNetML.NatNetClientML natNetClientML;

        //private Dictionary<FrameReady, NatNetML.FrameReadyEventHandler> frameReadyListeners = new Dictionary<FrameReady, NatNetML.FrameReadyEventHandler>();

        #endregion
        #region Events

        public event FrameReady OnFrameReady;
        //{
            //add
            //{
            //    var listener = new NatNetML.FrameReadyEventHandler(
            //        (NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client) => value(NatNetFrameOfMocapData.Create(data), this)
            //        );
            //    frameReadyListeners[value] = listener;
            //    natNetClientML.OnFrameReady += listener;
            //}
            //remove
            //{
            //    NatNetML.FrameReadyEventHandler listener = frameReadyListeners[value];
            //    frameReadyListeners.Remove(value);

            //    natNetClientML.OnFrameReady -= listener;
            //}
        //}

        #endregion
        #region Constructors

        public NatNetClientML(int connectionType)
        {
            //Windows.System.Threading.ThreadPool.RunAsync(
            //(IAsyncAction action) =>
            //{
            //});
            //..System.Threading.

            natNetClientML = new NatNetML.NatNetClientML(connectionType);
            natNetClientML.OnFrameReady += natNetClientML_OnFrameReady;

            //natNetClientML.OnFrameReady += new NatNetML.FrameReadyEventHandler(m_NatNet_OnFrameReady);
            //natNetClientML.OnFrameReady2 += new NatNetML.FrameReadyEventHandler2(OnFrameReady2);



        }

        private void natNetClientML_OnFrameReady(NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client)
        {
            if (OnFrameReady != null)
                OnFrameReady(NatNetFrameOfMocapData.Create(data), this);
        }

        //private void OnFrameReady2(object sender, NatNetML.NatNetEventArgs e)
        //{
        //    //m_NatNet_OnFrameReady(e.data, e.client);
        //   // log.Info("OnFrameReady2");
        //    Debug.WriteLine("OnFrameReady2");
        //}

        //void m_NatNet_OnFrameReady(NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client)
        //{
        //    //Console.WriteLine("m_NatNet_OnFrameReady");
        //    //// [optional] High-resolution frame arrival timing information
        //    //Int64 currTime = timer.Value;
        //    //if (lastTime != 0)
        //    //{
        //    //    // Get time elapsed in tenths of a millisecond.
        //    //    Int64 timeElapsedInTicks = currTime - lastTime;
        //    //    Int64 timeElapseInTenthsOfMilliseconds = (timeElapsedInTicks * 10000) / timer.Frequency;
        //    //    // uncomment for timing info
        //    //    //OutputMessage("Frame Delivered: (" + timeElapseInTenthsOfMilliseconds.ToString() + ")  FrameTimestamp: " + data.fLatency);
        //    //}

        //    //// [NatNet] Add the incoming frame of mocap data to our frame queue,  
        //    //// Note: the frame queue is a shared resource with the UI thread, so lock it while writing
        //    //lock (syncLock)
        //    //{
        //    //    // [optional] clear the frame queue before adding a new frame
        //    //    m_FrameQueue.Clear();
        //    //    m_FrameQueue.Enqueue(data);
        //    //}
        //    //lastTime = currTime;
        //}

        #endregion
        #region Methods

        protected static string VersionToString(int[] version)
        {
            if (version.Length < 4)
                throw new Exception("Version array dimension not as expected");

            return String.Format("{0}.{1}.{2}.{3}", version[0], version[1], version[2], version[3]);
        }

        //void m_NatNet_OnFrameReady(NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client)
        //{
        //    Debug.WriteLine("OnFrameReady");
        //}

        public int Initialize(String localAddress, String serverAddress)
        {
            return natNetClientML.Initialize(localAddress, serverAddress);
        }

        public int Uninitialize()
        {
            return natNetClientML.Uninitialize();
        }

        //public ServerDescription GetServerDescription()
        //{
        //    var desc = new NatNetML.ServerDescription();


        //    //if (!(desc is NatNetServerDescription))
        //    //    throw new ArgumentException();

        //    bool success = (natNetClientML.GetServerDescription(desc) == 0);
        //    if (!success)
        //        throw new Exception("Failed to retreieve server description");

        //    return NatNetServerDescription.Create(desc);

        //}

        public int GetServerDescription(ServerDescription desc)
        {
            if (!(desc is NatNetServerDescription))
                throw new ArgumentException();

            return natNetClientML.GetServerDescription((desc as NatNetServerDescription).serverDescription);
        }

        public int[] NatNetVersion()
        {
            return natNetClientML.NatNetVersion();
        }

        public int SendMessageAndWait(string message, out byte[] serverResponse, out int responseSize)
        {
            return natNetClientML.SendMessageAndWait(message, out serverResponse, out responseSize);
        }

        public List<DataDescriptor> GetDataDescriptions()
        {
            return natNetClientML.GetDataDescriptions().Select<NatNetML.DataDescriptor, DataDescriptor>(
                d => NatNetDataDescriptor.Create(d)
                ).ToList();
        }

        public FrameOfMocapData GetLastFrameOfData(bool processAsBroadcastFrame)
        {
            var skod = natNetClientML.GetLastFrameOfData();
            var frame = NatNetFrameOfMocapData.Create(skod);

            if (processAsBroadcastFrame && OnFrameReady != null)
                OnFrameReady(frame, this);

            return frame;
        }

        public bool GetDataDescriptions(out List<DataDescriptor> descriptions)
        {
            var natNetDescriptions = new List<NatNetML.DataDescriptor>();
            bool result = natNetClientML.GetDataDescriptions(out natNetDescriptions);

            descriptions = natNetDescriptions.Select<NatNetML.DataDescriptor, DataDescriptor>(
                d => NatNetDataDescriptor.Create(d)
                ).ToList();

            return result;
        }

        public bool DecodeTimecode(uint inTimecode, uint inTimecodeSubframe, out int hour, out int minute, out int second, out int frame, out int subframe)
        {
            return natNetClientML.DecodeTimecode(inTimecode, inTimecodeSubframe, out hour, out minute, out second, out frame, out subframe);
        }

        #endregion
    }
}
