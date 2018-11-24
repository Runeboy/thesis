using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.ObjectModel;
using WindowsPreview.Kinect;
using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.Data.Dto;
using System.Collections.Generic;

namespace Airswipe.WinRT.Kinect
{
    public class SimplifiedKinectClient : SimplifiedMotionTrackerClient
    {
        #region Fields

        private ILogger log = new TypeLogger<SimplifiedKinectClient>();

        private readonly JointType pointerJointType;

        private Body[] bodies;
        private BodyFrameReader bodyFrameReader;
        private KinectSensor kinectSensor;

        private const bool ALLOW_INFERRED = false;

        //private Queue<OffscreenPoint> pointQueue = new Queue<OffscreenPoint>();

        public event ConnectSucceeded OnConnectSucceeded;

        public event DataDescriptionReady OnDataDescriptionReady
        {
            add
            {
                log.Verbose("ignoring event subscription add for Kinect OnDataDescriptionReady ");
            }
            remove
            {
                throw new NotImplementedException();
            }
        }

        public event DisconnectComplete OnDisconnectComplete;
        //public event FrameBatchReady OnFrameBatchReady; // { add { throw new NotImplementedException(); } remove { throw new NotImplementedException(); }  }  
        public event TrackedPointReadyHandler TrackedPointReady;


        private OffscreenPoint lastHandTipPoint = new OffscreenPoint();
        private SpatialPoint lastWristPoint = new XYZPoint();
        private SpatialPoint lastSphereCenterPoint = new XYZPoint();

        #endregion
        #region Constructor

        public SimplifiedKinectClient(JointType _pointerJointType, bool useSmoothing, double smoothingBase, double smoothingCutoffMilliSeconds, double smoothingDeltaMultiplier)
        {
            pointerJointType = _pointerJointType;
            UseSmoothing = useSmoothing;

            SmoothingBase = smoothingBase;
            SmoothingCutoffMilliSeconds = smoothingCutoffMilliSeconds;
            SmoothingDeltaMultiplier = smoothingDeltaMultiplier;
        }

        #endregion
        #region Methods

        public void Connect(string strLocalIP, string strServerIP)
        {
            // one sensor is currently supported
            kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            //this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            //this.JointSpaceWidth = frameDescription.Width;
            //this.JointSpaceHeight = frameDescription.Height;

            bodies = new Body[kinectSensor.BodyFrameSource.BodyCount];
            bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();

            bodyFrameReader.FrameArrived += Reader_BodyFrameArrived;
            kinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            kinectSensor.Open();

            kinectSensor.IsAvailableChanged += (KinectSensor sender, IsAvailableChangedEventArgs args) =>
            {
                if (kinectSensor.IsAvailable)
                    if (OnConnectSucceeded != null)
                        OnConnectSucceeded(null);
            };


            //// set the status text
            //kinectInfo.StatusText = this.kinectSensor.IsAvailable ? "Running"
            //                                                : "No kinect found!";
        }

        public void Disconnect()
        {
            DisconnectAndUninitializeKinectConnection();
            if (OnDisconnectComplete != null)
                OnDisconnectComplete(false);
        }

        public void GetDataDescriptions()
        {
            throw new NotImplementedException();
        }

        public FrameOfMocapData GetLastFrameOfData(bool processAsBroadcastFrame)
        {
            throw new NotImplementedException();
        }

        public void SimulateFrameReceival(FrameOfMocapData frame)
        {
            //frame.
            //HandleFrameReceived(frame, this);

            throw new NotImplementedException();
        }

        private void DisconnectAndUninitializeKinectConnection()
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReder is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            // Body is IDisposable
            if (this.bodies != null)
            {
                foreach (Body body in this.bodies)
                {
                    if (body != null)
                    {
                        body.Dispose();
                    }
                }
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        #endregion
        #region Event handlers

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            if (!kinectSensor.IsAvailable)
            {
                //this.StatusText = resourceLoader.GetString("SensorNotAvailableStatusText");
            }
            else
            {
                //this.StatusText = resourceLoader.GetString("RunningStatusText");
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            bool hasTrackedBody = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                // iterate through each body
                for (int bodyIndex = 0; bodyIndex < this.bodies.Length; bodyIndex++)
                {
                    //OnFrameBatchReady()
                    Body body = this.bodies[bodyIndex];

                    if (body.IsTracked)
                    {
                        //Joint handTipJoin = body.Joints[JointType.HandTipRight];
                        //Joint wristJoint = body.Joints[JointType.HandRight];

                        Joint handTipJoint = body.Joints[JointType.HandTipRight];
                        //Joint wristJoint = body.Joints[JointType.HandRight];
                        Joint handJoint = body.Joints[JointType.HandRight]; //HandRight];

                        //Joint sphereCenterJoint = body.Joints[JointType.ShoulderRight]; //HandRight];
                        Joint sphereCenterJoint = body.Joints[JointType.ShoulderRight]; //HandRight];

                        bool isFirstTrackedBody = (hasTrackedBody == false);
                        if (isFirstTrackedBody)
                        {
                            FrameCount++;

                            if (ALLOW_INFERRED || (handTipJoint.TrackingState == TrackingState.Tracked)) // assume wrist may be correctly inferred if if hand tip  is tracked
                            {
                                DateTime captureTime = DateTime.Now;
                                TimeSpan timeSinceLastPoint = (captureTime - lastHandTipPoint.CaptureTime);

                                SpatialPoint newHandTipPoint = new XYZPoint
                                {
                                    X = handTipJoint.Position.X,
                                    Y = handTipJoint.Position.Y,
                                    Z = handTipJoint.Position.Z
                                };

                                SpatialPoint newWristPoint = new XYZPoint
                                {
                                    X = handJoint.Position.X,
                                    Y = handJoint.Position.Y,
                                    Z = handJoint.Position.Z
                                };

                                SpatialPoint newSphereCenterPoint = new XYZPoint
                                {
                                    X = sphereCenterJoint.Position.X,
                                    Y = sphereCenterJoint.Position.Y,
                                    Z = sphereCenterJoint.Position.Z
                                };

                                var handTipDelta = newHandTipPoint.Subtract(lastHandTipPoint);
                                var wristDelta = newWristPoint.Subtract(lastWristPoint);
                                var sphereCenterDelta = newSphereCenterPoint.Subtract(lastSphereCenterPoint);

                                if (UseSmoothing)
                                {
                                    newHandTipPoint = ApplySmoothing(timeSinceLastPoint.TotalMilliseconds, newHandTipPoint, handTipDelta);
                                    newWristPoint = ApplySmoothing(timeSinceLastPoint.TotalMilliseconds, newWristPoint, wristDelta);
                                    newSphereCenterPoint = ApplySmoothing(timeSinceLastPoint.TotalMilliseconds, newSphereCenterPoint, sphereCenterDelta);
                                }

                                lastWristPoint = newWristPoint;
                                lastSphereCenterPoint = newSphereCenterPoint;

                                var direction = newHandTipPoint.Subtract(newWristPoint).Normalize();
                                //log.Verbose(direction.ToString());

                                lastHandTipPoint = new OffscreenPoint
                                {
                                    CaptureTime = captureTime,
                                    Confidence = ConvertConfidence(handTipJoint.TrackingState),
                                    X = newHandTipPoint.X,
                                    Y = newHandTipPoint.Y,
                                    Z = newHandTipPoint.Z,
                                    Delta = handTipDelta,
                                    Direction = direction,
                                    SphereCenter = newSphereCenterPoint
                                };



                                //pointQueue.Enqueue(lastPoint);

                                //PurgeCompleted(); 

                                NotifyPointReceived(lastHandTipPoint);


                            }
                        }
                        hasTrackedBody = true;
                    }
                }
            }
        }

        private SpatialPoint ApplySmoothing(double msSinceLastPoint, SpatialPoint newPoint, SpatialPoint pointDelta)
        {
                double ALPHA =
                    //Math.Pow(SmoothingDeltaMultiplier, -delta.Length)
                    Math.Max(0, 1 - SmoothingDeltaMultiplier * Math.Pow(pointDelta.Length, 2)) // polynomial for responsive acceleration
                    *
                    Math.Pow(
                    SmoothingBase, //+, 
                    -msSinceLastPoint
                    );

                //if (delta.Length > 40) { }

                newPoint = newPoint.Subtract(
                    pointDelta.Multiply(ALPHA)
                    );


                //double ALPHA = Math.Max(0, 0.5 + SmoothingBase * (-msSinceLastPoint / 30));
                //double ALPHA = Math.Max(0, 
                //0.5 + SmoothingBase * (-msSinceLastPoint / 30)
                //    );


                //SpatialPoint smoothedPoint = newPoint.Multiply(1);

                //foreach (var framePoint in pointQueue) {
                //    var duration = (captureTime - framePoint.CaptureTime);

                //    if (duration.TotalMilliseconds < SmoothingCutoffMilliSeconds)
                //    {
                //        //log.Verbose(ms.ToString());
                //        var ALPHA = Math.Pow(SmoothingBase, -duration.TotalMilliseconds);

                //        smoothedPoint = smoothedPoint.Add(
                //            (framePoint.Subtract(smoothedPoint)).Multiply(ALPHA)
                //            );
                //    }
                //}

                //newPoint = smoothedPoint;
                //lastPoint.X = smoothedPoint.X;
                //lastPoint.Y = smoothedPoint.Y;
                //lastPoint.Z = smoothedPoint.Z;
            return newPoint;
        }

        //private async void PurgeCompleted()
        //{
        //    while (pointQueue.Count > 0 && (DateTime.Now - pointQueue.Peek().CaptureTime).TotalMilliseconds > SmoothingCutoffMilliSeconds)
        //        pointQueue.Dequeue();
        //}

        private void NotifyPointReceived(OffscreenPoint point)
        {
            //var delta = (LastPoint == null ?
            //    new XYPoint() :
            //    LastPoint.Subtract(point)
            //    );
            //point.Subtract(LastPoint)

            //LastPoint = point;

            if (TrackedPointReady != null)
                TrackedPointReady(new OffscreenPoint[] { point });
        }

        private PointTrackingConfidence ConvertConfidence(TrackingState trackingState)
        {
            switch (trackingState)
            {
                case TrackingState.Tracked: return PointTrackingConfidence.Tracked;
                case TrackingState.NotTracked: return PointTrackingConfidence.NotTracked;
                case TrackingState.Inferred: return PointTrackingConfidence.Inferred;
            }
            throw new Exception("Cannot convert confidence.");
        }

        #endregion
        #region Properties

        public DateTime? ConnectTime { get; set; }

        public long FrameCount { get; set; }

        public ObservableCollection<MarkerSet> MarkerSets { get; set; }

        public ObservableCollection<RigidBody> RigidBodies { get; set; }

        public ObservableCollection<Skeleton> Skeletons { get; set; }

        public string VersionString { get; set; }

        public OffscreenPoint LastPoint { get { return lastHandTipPoint; } }

        public bool UseSmoothing { get; private set; }
        public double SmoothingBase { get; set; }

        public double SmoothingCutoffMilliSeconds { get;  set; }
        public double SmoothingDeltaMultiplier { get; set; }

        //private OffscreenPoint lastPoint = new OffscreenPoint();

        #endregion
    }
}
