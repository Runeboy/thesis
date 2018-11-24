using Airswipe.WinRT.Core;
using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.UI.Common;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using System.Linq;
using Airswipe.WinRT.NatNetPortable;

namespace Airswipe.WinRT.UI.Pages
{
    public sealed partial class DistanceMeasurePage : BasicPage
    {
        #region Fields

        private static readonly ILogger log = new TypeLogger<DistanceMeasurePage>();

        private static readonly Color[] lineDistanceColors = new Color[] { Colors.Red, Colors.Blue, Colors.Green, Colors.Purple, Colors.Yellow, Colors.Azure, Colors.Pink, Colors.LightGreen, Colors.Brown };
        private int distanceUpperLimit = AppSettings.DistanceUpperLimit;
        int ridigBodyIDReference = AppSettings.DistanceMeasureRidigBodyIDReference;
        bool calculateRidigBodyStats = AppSettings.CalculateRidigBodyStats;

        private readonly SimplifiedNatNetPortableMotionTrackerClient motionTrackerClient;

        #endregion
        #region Constructors 

        public DistanceMeasurePage()
        {
            InitializeComponent();

            if (!(AppMotionTrackerClient.Instance is SimplifiedNatNetPortableMotionTrackerClient))
                throw new Exception("Current motion tracker is not OptiTrack.");

            motionTrackerClient = (SimplifiedNatNetPortableMotionTrackerClient)AppMotionTrackerClient.Instance;

            ListenForFrameBatchesReceived();
        }

        #endregion
        #region Event handlers

        private void ListenForFrameBatchesReceived()
        {
            Loaded += (s, e) => motionTrackerClient.OnFrameBatchReady += Instance_OnFrameBatchReady;
        }

        private void Instance_OnFrameBatchReady(List<FrameOfMocapData> data, SimplifiedMotionTrackerClient client)
        {
            DistanceCanvas.Children.Clear();

            if (data.Count == 0)
                return;

            var frame = data[0];

            if (frame.RigidBodies.Length == 0)
            {
                log.Error("No ridig bodies in data frame");
                return;
            }
            if (frame.RigidBodies.Length < ridigBodyIDReference+1 )
            {
                log.Error("Invalid rigid body ID reference");
                return;
            }

            var distanceScreenUnit = DistanceCanvas.ActualWidth / distanceUpperLimit;

            RigidBodyData ridigBodyRef = frame.RigidBodies.Where(r => r.ID == ridigBodyIDReference).Single();
            

            foreach(var marker in frame.OtherMarkers)
            {
                var distanceCentimeter = 100 * MarkerDistanceMeter(ridigBodyRef, marker);

                var x = distanceCentimeter * distanceScreenUnit;

                Line line = new Line()
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = DistanceCanvas.ActualHeight,
                    StrokeThickness = 5,
                    Stroke = new SolidColorBrush(lineDistanceColors[marker.ID])
                };

                DistanceCanvas.Children.Add(line);

                var tb = new TextBlock { Text = marker.ID.ToString() };
                Canvas.SetLeft(tb, x + 10);
                Canvas.SetTop(tb, marker.ID * 5);

                DistanceCanvas.Children.Add(tb);

                //log.Verbose("D: "+distance + "");
            }

            if (calculateRidigBodyStats)
            {
                var distances = new List<double>(data.Count -1);

                RigidBodyData lastRb = null;
                foreach (var f in data)
                {
                    //log.Verbose("x: " + data.Count);

                    RigidBodyData rb = f.RigidBodies.Where(r => r.ID == ridigBodyIDReference).Single();
                    if (lastRb != null ) { 
                        distances.Add(MarkerDistanceMeter(rb, lastRb));
                        //log.Verbose(MarkerDistanceMeter(rb, lastRb) + "");
                    }

                    lastRb = rb;
                }

                double mean = distances.Sum() / distances.Count;
                double std = distances.Sum(d => Math.Pow(d - mean, 2)) / distances.Count;
                RigidBodyStatsTextBlock.Text = String.Format("Mean: {0}, Std: {1}", mean, std);
            }
        }

        private double MarkerDistanceMeter(Marker m1, Marker m2)
        {
            return Math.Sqrt(
                    Math.Pow(m1.X - m2.X, 2) +
                    Math.Pow(m1.Y - m2.Y, 2) +
                    Math.Pow(m1.Y - m2.Y, 2)
                    );
        }

        #endregion
        #region Properties

        public bool AutoConnectOnStartup
        {
            get { return AppSettings.AutoConnectOnStartup; }
            set { AppSettings.AutoConnectOnStartup = value; }
        }
        
        public string RidigBodyIDReference
        {
            get { return ridigBodyIDReference.ToString(); }
            set {
                ridigBodyIDReference = int.Parse(value);

                AppSettings.DistanceMeasureRidigBodyIDReference = ridigBodyIDReference;
            }
        }

        //public string DistanceReferenceMarkerName
        //{
        //    get { return AppSettings.DistanceMarkerName; }
        //    set { AppSettings.DistanceMarkerName = value; }
        //}

        public string DistanceUpperLimitCm
        {
            get { return distanceUpperLimit.ToString(); }
            set {
                distanceUpperLimit = int.Parse(value);

                AppSettings.DistanceUpperLimit = distanceUpperLimit;
            }
        }

        public bool CalculateRidigBodyStats
        {
            get { return calculateRidigBodyStats; }
            set {
                calculateRidigBodyStats = value;
                AppSettings.CalculateRidigBodyStats = value;

                RigidBodyStatsTextBlock.Visibility = value ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        #endregion
    }
}
