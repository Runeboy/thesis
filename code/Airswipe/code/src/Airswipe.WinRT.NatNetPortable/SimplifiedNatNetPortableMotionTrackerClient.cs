using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.Core.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using System.Collections.ObjectModel;
using Windows.Foundation;

namespace Airswipe.WinRT.NatNetPortable
{
    public class SimplifiedNatNetPortableMotionTrackerClient : NatNetClientML, SimplifiedMotionTrackerClient
    {
        #region Fields

        private ILogger log = new TypeLogger<SimplifiedNatNetPortableMotionTrackerClient>();

        // [NatNet] Our NatNet Frame of Data object
        //        private FrameOfMocapData m_FrameOfData = new NatNetFrameOfMocapData();

        // [NatNet] Description of the Active Model List from the server (e.g. Motive)
        private ServerDescription serverDescription = new NatNetServerDescription();

        public readonly ConnectionType ConnectionType;

        // [NatNet] Queue holding our incoming mocap frames the NatNet server (e.g. Motive)
        private readonly Queue<FrameOfMocapData> frameQueue = new Queue<FrameOfMocapData>();

        // spreadsheet lookup
        //public Dictionary<int, int> htMarkers = new Dictionary<int, int>();
        //public Dictionary<int, int> htRigidBodies = new Dictionary<int, int>();

        private readonly ObservableCollection<RigidBody> rigidBodies = new ObservableCollection<RigidBody>();
        private readonly ObservableCollection<MarkerSet> markerSets = new ObservableCollection<MarkerSet>();
        private readonly ObservableCollection<Skeleton> skeletons = new ObservableCollection<Skeleton>();

        // graphing support
        public const int GraphFrames = 500;


        public long FrameCount { get; private set; }
        //public int FramesPerSecond { get; private set; }

        public DateTime? ConnectTime { get; private set; }

        public int m_iLastFrameNumber = 0;

        // frame timing information
        public double lastFrameTimestamp = 0.0f;
        public float currentFrameTimestamp = 0.0f;
        public float m_fFirstMocapFrameTimestamp = 0.0f;
        //HiResTimer timer;
        public long lastTime = 0;

        // server information
        public double m_ServerFramerate = 1.0f;
        public float m_ServerToMillimeters = 1.0f;

        private static object syncLock = new object();
        private delegate void OutputMessageCallback(string strMessage);
        //private bool needMarkerListUpdate = false;

        private const int TIMER_TICK_MILLISECONDS = 250;
        private DispatcherTimer frameBatchDequeueTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(TIMER_TICK_MILLISECONDS) };

        private readonly int pointerMarkerID;

        #endregion
        #region Events

        public event DisconnectComplete OnDisconnectComplete;
        public event ConnectSucceeded OnConnectSucceeded;
        public event DataDescriptionReady OnDataDescriptionReady;
        public event FrameBatchReady OnFrameBatchReady;
        //public event FrameReady OnFrameReady;
        public event TrackedPointReadyHandler TrackedPointReady;// { add { throw new NotImplementedException(); } remove { throw new NotImplementedException(); } }


        #endregion
        #region Constructors

        public SimplifiedNatNetPortableMotionTrackerClient(ConnectionType connectionType, int _pointerMarkerID, double smoothingBase)
            : base(GetConnectionTypeId(connectionType))
        {
            log.Info("construct (as {0} client)", connectionType);

            pointerMarkerID = _pointerMarkerID;
            ConnectionType = connectionType;
            SmoothingBase = smoothingBase;

            IntializeFrameHandling();
            InitializeFrameBatchHandling();
        }

        #endregion
        #region Methods

        public void SimulateFrameReceival(FrameOfMocapData frame)
        {
            HandleFrameReceived(frame, this);
        }
    
        private void IntializeFrameHandling()
        {
            OnFrameReady += new FrameReady(HandleFrameReceived);
        }

        private void InitializeFrameBatchHandling()
        {
            frameBatchDequeueTimer.Tick += HandleFrameBatchDequeueTimerTick;
            frameBatchDequeueTimer.Start();

            log.Verbose("batch timer started");
        }   

        public void GetDataDescriptions()
        {
            rigidBodies.Clear();
            markerSets.Clear();
            skeletons.Clear();
            //   dataGridView1.Rows.Clear();
            //needMarkerListUpdate = true;

            log.Info("Retrieving data descriptions..");

            //OutputMessage("Retrieving Data Descriptions....");
            var descriptors = new List<DataDescriptor>();
            if (!GetDataDescriptions(out descriptors))
            {
                log.Error("Unable to retrieve DataDescriptions");

                return;
            }

            log.Info(String.Format("Retrieved {0} Data Descriptions....", descriptors.Count));
            //            int iObject = 0;
            foreach (NatNetDataDescriptor descriptor in descriptors)
            {
                //              iObject++;

                // MarkerSets
                if (descriptor is MarkerSet)
                //if (d.Type == (int)DataDescriptorType.eMarkerSetData)
                {
                    MarkerSet markerSet = (MarkerSet)descriptor;
                    //log.Info("Data Def " + iObject.ToString() + " [MarkerSet]");

                    //log.Info(" Name : " + markerSet.Name);
                    //log.Info(String.Format(" Markers ({0}) ", markerSet.MarkerNames.Count));
                    ////dataGridView1.Rows.Add("MarkerSet: " + ms.Name);
                    //for (int i = 0; i < markerSet.NMarkers; i++)
                    //foreach(string markerName in markerSet.MarkerNames)
                    //{
                    //log.Info("  " + markerName);
                    //int rowIndex = dataGridView1.Rows.Add("  " + ms.MarkerNames[i]);
                    // MarkerNameIndexToRow map
                    //String strUniqueName = markerSet.Name + i.ToString();
                    //int key = strUniqueName.GetHashCode();
                    //htMarkers.Add(key, rowIndex); 
                    //}

                    markerSets.Add(markerSet);
                }
                // RigidBodies
                //else if (d.Type == (int)DataDescriptorType.eRigidbodyData)
                else if (descriptor is RigidBody)
                {
                    RigidBody rigidBody = (RigidBody)descriptor;

                    //log.Info("Data Def " + iObject.ToString() + " [RigidBody]");
                    //log.Info(" Name : " + rigidBody.Name);
                    //log.Info(" ID : " + rigidBody.ID);
                    //log.Info(" ParentID : " + rigidBody.ParentID);
                    //log.Info(" OffsetX : " + rigidBody.Offsetx);
                    //log.Info(" OffsetY : " + rigidBody.Offsety);
                    //log.Info(" OffsetZ : " + rigidBody.Offsetz);

                    ////int rowIndex = dataGridView1.Rows.Add("RigidBody: "+rb.Name);
                    //// RigidBodyIDToRow map
                    ////int key = rigidBody.ID.GetHashCode();
                    ////htRigidBodies.Add(key, rowIndex);

                    rigidBodies.Add(rigidBody);

                }
                // Skeletons
                //else if (d.Type == (int)DataDescriptorType.eSkeletonData)
                else if (descriptor is Skeleton)
                {
                    Skeleton skeleton = (Skeleton)descriptor;

                    //log.Info("Data Def " + iObject.ToString() + " [Skeleton]");
                    //log.Info(" Name : " + skeleton.Name);
                    //log.Info(" ID : " + skeleton.ID);
                    ////dataGridView1.Rows.Add("Skeleton: " + sk.Name);
                    //foreach(RigidBody rb in skeleton.RigidBodies)
                    ////for (int i = 0; i < skeleton.NRigidBodies; i++)
                    //{
                    //  //  RigidBody rb = skeleton.RigidBodies[i];
                    //    log.Info(" RB Name  : " + rb.Name);
                    //    log.Info(" RB ID    : " + rb.ID);
                    //    log.Info(" ParentID : " + rb.ParentID);
                    //    log.Info(" OffsetX  : " + rb.Offsetx);
                    //    log.Info(" OffsetY  : " + rb.Offsety);
                    //    log.Info(" OffsetZ  : " + rb.Offsetz);
                    //    //int rowIndex = dataGridView1.Rows.Add("Bone: " + rb.Name);
                    //    // RigidBodyIDToRow map
                    //    //int uniqueID = sk.ID * 1000 + rb.ID;
                    //    //int key = uniqueID.GetHashCode();
                    //    //if (htRigidBodies.ContainsKey(key))
                    //    //    ShowMessageDialog("Duplicate RigidBody ID");
                    //    //else
                    //    //    htRigidBodies.Add(key, rowIndex);
                    //}

                    skeletons.Add(skeleton);
                }
                else
                {
                    log.Error("Unknown DataType '{O}'", descriptor.GetType());
                }
            }

            if (OnDataDescriptionReady != null)
                OnDataDescriptionReady(markerSets, rigidBodies, skeletons);
        }

        /// <summary>
        /// Connect to a NatNet server (e.g. Motive)
        /// </summary>
        public void Connect(string strLocalIP, string strServerIP)
        {
            log.Info("connecting");

            // [NatNet] connect to a NatNet server
            int returnCode = 0;
            returnCode = Initialize(strLocalIP, strServerIP);

            if (returnCode != 0)
            {
                string errorMessage = String.Format("Connect initialization failed (return code {0})", returnCode);
                log.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            // [NatNet] validate the connection
            returnCode = GetServerDescription(serverDescription);
            if (returnCode != 0)
            {
                String errorMessage = String.Format("Failed to retrieve server description (code {0}) following successful initialization", returnCode);
                log.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            //serverDescription = GetServerDescription();

            log.Info("Retrieved server description");
            log.Verbose("   Server App Name: " + serverDescription.HostApp);

            log.Verbose(String.Format("   Server App Version: " + VersionToString(serverDescription.HostAppVersion)));
            log.Verbose(String.Format("   Server NatNet Version: " + VersionToString(serverDescription.NatNetVersion)));

            //log.Verbose(String.Format("   Server App Version: {0}.{1}.{2}.{3}", serverDescription.HostAppVersion[0], serverDescription.HostAppVersion[1], serverDescription.HostAppVersion[2], serverDescription.HostAppVersion[3]));
            //log.Verbose(String.Format("   Server NatNet Version: {0}.{1}.{2}.{3}", serverDescription.NatNetVersion[0], serverDescription.NatNetVersion[1], serverDescription.NatNetVersion[2], serverDescription.NatNetVersion[3]));
            //checkBoxConnect.Text = "Disconnect";

            // Tracking Tools and Motive report in meters - lets convert to millimeters

            HostAppIsTrackingToolsorMotive = (serverDescription.HostApp.Contains("TrackingTools") || serverDescription.HostApp.Contains("Motive"));
            bool hostAppReportsInMetersInsteadOfMillimeters = HostAppIsTrackingToolsorMotive;
            if (hostAppReportsInMetersInsteadOfMillimeters)
                m_ServerToMillimeters = 1000.0f;

            // [NatNet] [optional] Query mocap server for the current camera framerate
            int nBytes = 0;
            byte[] response = new byte[10000];
            //int rc;
            returnCode = SendMessageAndWait("FrameRate", out response, out nBytes);
            if (returnCode == 0)
            {
                try
                {
                    m_ServerFramerate = BitConverter.ToSingle(response, 0);
                    log.Verbose(String.Format("Succeeded in getting server framerate: {0}", m_ServerFramerate));
                }
                catch (System.Exception ex)
                {
                    log.Verbose("Failed to retrieve server framerate: " + ex.Message);
                }
            }

            currentFrameTimestamp = 0.0f;
            m_fFirstMocapFrameTimestamp = 0.0f;

            ConnectTime = DateTime.Now;
            if (OnConnectSucceeded != null)
                OnConnectSucceeded(serverDescription);
        }

        public void Disconnect()
        {
            log.Info("disconnecting");

            bool error = (Uninitialize() != 0);

            if (OnDisconnectComplete != null)
                OnDisconnectComplete(error);
        }

        #endregion
        #region Helper methods

        private static int GetConnectionTypeId(ConnectionType connectionType)
        {
            switch (connectionType)
            {
                case ConnectionType.Multicast: return 0;
                case ConnectionType.Unicast: return 1;
            }

            throw new ArgumentException("Connection type " + connectionType + " not implemented.");
        }

        #endregion
        #region Event handler methods

        private void HandleFrameReceived(FrameOfMocapData data, MotionTrackerClient client)
        {
            if (data == null)
            {
                log.Error("Skipping frame as it was null (??)");

                return;
            }

            //log.Verbose("frame delivered");

            // [optional] High-resolution frame arrival timing information
            long currTime = Stopwatch.GetTimestamp();

            if (lastTime != 0)
            {
                // Get time elapsed in tenths of a millisecond.
                long timeElapsedInTicks = currTime - lastTime;
                long timeElapseInTenthsOfMilliseconds = (timeElapsedInTicks * 10000) / Stopwatch.Frequency;
                // uncomment for timing info



                //log.Verbose("Frame Delivered: (" + timeElapseInTenthsOfMilliseconds.ToString() + ")  FrameTimestamp: " + data.fLatency);
            }

            if (HostAppIsTrackingToolsorMotive)
            {
                currentFrameTimestamp = data.fLatency;
                if (currentFrameTimestamp == lastFrameTimestamp)
                {
                    log.Info("Skipping frame as the timestamp is identical to that of the last frame received");
                    Debug.WriteLine("--Skipping frame as the timestamp is identical to that of the last frame received");
                    return;
                }
                if (m_fFirstMocapFrameTimestamp == 0.0f)
                {
                    m_fFirstMocapFrameTimestamp = currentFrameTimestamp;
                }
                data.iFrame = (int)((currentFrameTimestamp - m_fFirstMocapFrameTimestamp) * m_ServerFramerate);

            }
                
            // [NatNet] Add the incoming frame of mocap data to our frame queue,  
            // Note: the frame queue is a shared resource with the UI thread, so lock it while writing
            lock (syncLock)
            {
                FrameCount++; // will in fact get unprecise by simultaneous calls if not guarded against race conditions

                // [optional] clear the frame queue before adding a new frame
                //frameQueue.Clear();
                frameQueue.Enqueue(data);

                lastTime = currTime;
            }

            //if (1==2 && )
            //    if (OnFrameReady != null)
            //    {
            //        foreach (var frame in frames)
            //            PostProcessFrame(frame);
            //        OnFrameBatchReady(frames, this);
            //    }

        }

        private void HandleFrameBatchDequeueTimerTick(object sender, object e)
        {
            // the frame queue is a shared resource with the FrameOfMocap delivery thread, so lock it while reading
            // note this can block the frame delivery thread.  In a production application frame queue management would be optimized.
            List<FrameOfMocapData> frames;

            lock (syncLock)
            {
                //log.Verbose("tick");
                //IFrameOfMocapData m_FrameOfData;
                if (frameQueue.Count == 0)
                    return;

                frames = new List<FrameOfMocapData>(frameQueue.Count);

                while (frameQueue.Count > 0)
                {
                    FrameOfMocapData frame = frameQueue.Dequeue();
                    //if (frameQueue.Count > 0)
                    //    continue;

                    frames.Add(frame);

                }

                //StatsTextBlock.Text = String.Format("Frame count: {0}\n", m_FrameCounter);
            }

            NotifyListenersFrameBatchReceived(frames);
        }

        private void NotifyListenersFrameBatchReceived(List<FrameOfMocapData> frames)
        {
            foreach (var frame in frames) { 
                PostProcessFrame(frame);

                OffscreenPoint[] points = frames.Select(f => FrameToTrackedPoint(f)).ToArray();
                if (points.Length > 0)
                    LastPoint = points[0];
                    
                if (TrackedPointReady != null)
                    TrackedPointReady(points);
            }


            if (OnFrameBatchReady != null)
                OnFrameBatchReady(frames, this);

        }

        private OffscreenPoint FrameToTrackedPoint(FrameOfMocapData f)
        {
            foreach (Marker marker in f.OtherMarkers)
                if (marker.ID == pointerMarkerID)
                    return new OffscreenPoint
                    {
                        CaptureTime = DateTime.Now,
                        Confidence = PointTrackingConfidence.Tracked,
                        X = marker.X, Y = marker.Y, Z = marker.Z
                    };

            throw new Exception("Frame does not contain a tracked-pointer-marker with ID " + pointerMarkerID + " (bad configuration, check settings).");
        }

        private void PostProcessFrame(FrameOfMocapData frame2)
        {
            // for servers that only use timestamps, not frame numbers, calculate a 
            // frame number from the time delta between frames

            // update the data grid
            //UpdateDataGrid();

            // update the chart
            //UpdateChart(m_FrameCounter++);

            // only redraw chart when necessary, not for every frame
            //if (m_FrameQueue.Count == 0)
            //{
            //    chart1.ChartAreas[0].RecalculateAxesScale();
            //    chart1.ChartAreas[0].AxisX.Minimum = 0;
            //    chart1.ChartAreas[0].AxisX.Maximum = GraphFrames;
            //    chart1.Invalidate();
            //}

            // Mocap serverz timestamp (in seconds)
            lastFrameTimestamp = frame2.fTimestamp;
            string timestampValue = frame2.fTimestamp.ToString("F3");

            // SMPTE timecode (if timecode generator present)
            int hour, minute, second, frame, subframe;
            bool bSuccess = DecodeTimecode(frame2.Timecode, frame2.TimecodeSubframe, out hour, out minute, out second, out frame, out subframe);
            string timecodeValue;
            if (bSuccess)
                timecodeValue = string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}.{4:D2}", hour, minute, second, frame, subframe);

            //if (m_FrameOfData.bRecording)
            //    chart1.BackColor = Color.Red;
            //else
            //    chart1.BackColor = Color.White;

        }

        #endregion
        #region Properties

        public string VersionString
        {
            get { return VersionToString(NatNetVersion()); }
        }

        public OffscreenPoint LastPoint { get; private set; }

        private bool HostAppIsTrackingToolsorMotive { get; set; }

        public ObservableCollection<RigidBody> RigidBodies { get { return rigidBodies; } }

        public ObservableCollection<Skeleton> Skeletons { get { return skeletons; } }

        public ObservableCollection<MarkerSet> MarkerSets { get { return markerSets; } }

        public double SmoothingBase { get; set; }

        public double SmoothingCutoffMilliSeconds { get; set; }

        public double SmoothingDeltaMultiplier { get; set; }

        #endregion
    }
}
