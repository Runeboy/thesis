using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Windows.Foundation;
using WindowsPreview.Kinect;
using Windows.System;
using Airswipe.WinRT.Core.Log;

namespace Airswipe.WinRT.Core
{
    public class AppSettings
    {
        #region Fields

        private static readonly ILogger log = new TypeLogger<AppSettings>();

        public static class Exp3Skew
        {
            public static readonly double Angle = 0;
            public static readonly Point Scale = new Point(1, 1);
        }

        private static InputSpace inputSpace = null;

        public static readonly TrackerTypes TrackerType = TrackerTypes.Kinect; // hardcoded setting

        public const int UPPER_TICK_VALUE = 50;

        public const int CLIENT_STAT_UPDATE_INTERVAL = 150;

        public const int MIN_EXPECTED_FPS = 20;

        public const int OFFSCREEN_TAP_RELEASE_PROJECTION_DISTANCE_MILLIMETER = 220;

        //public const VirtualKey OFFSCREEN_TAP_RELEASE_KEY = VirtualKey.Control;


        //public static readonly Point DisplayDimensionMilliMeters = new Point(292.1, 201.5);

        public const double DISPLAY_PIXELS_PER_INCH = 216.33;
        public const double DISPLAY_PIXELS_PER_MILLIMETER = 8.5169291338582677165354330708661;

        private const double MM_PER_INCH = 25.4;

        public const double DISPLAY_PIXELS_PER_MM = DISPLAY_PIXELS_PER_INCH / MM_PER_INCH;

        public static bool IS_KINECT_SMOOTHING_ENABLED = true;

        public const double INERTIA_FACTOR = 2.7;

        public const double MAX_SWIPE_VELOCITY = 7.3;


        private static Exp3PointMapper exp3PCAData = null;

        //internal static readonly double BULLSEYE_RADIUS_MILLIMETERS = 20;


        //public const double ProjectionPointExpAvgSmoothing = 0.5;

        #endregion
        #region Methods

        public static double MilliMeterToOnscreenDistance(double mm)
        {
            return DISPLAY_PIXELS_PER_MM * mm;
        }

        public static double MilliMeterToOffscreenDistance(double mm)
        {
            return InputSpace.OnscreenDistanceToOffscreenDistance(
                MilliMeterToOnscreenDistance(mm)
                );
        }

        public static double OnscreenDistanceToMillimeter(double d)
        {
            return d / DISPLAY_PIXELS_PER_MM;
        }

        public static double OffscreenDistanceToMillimeter(double d)
        {
            return OnscreenDistanceToMillimeter(
                InputSpace.OffscreenDistanceToOnscreenDistance(d)
                );
        }

        #endregion
        #region Properties

        public static double SphereOffscreenRadius
        {
            get { return MilliMeterToOffscreenDistance(SphereMillimeterRadiusFromScreenCenter); }
        }

        public static double SphereMillimeterRadiusFromScreenCenter
        {
            get { return double.Parse(StorageExpert.GetSetting("SphereMillimeterRadiusFromScreenCenter") ?? "500"); }
            set { StorageExpert.SaveSettings("SphereMillimeterRadiusFromScreenCenter", value.ToString()); }
        }

        public static bool AutoConnectOnStartup
        {
            get { return bool.Parse(StorageExpert.GetSetting("AutoConnectOnStartup") ?? bool.FalseString); }
            set { StorageExpert.SaveSettings("AutoConnectOnStartup", value.ToString()); }
        }

        public static string SelectedLocalNetworkAddress
        {
            get { return StorageExpert.GetSetting("SelectedLocalNetworkAddress"); }
            set { StorageExpert.SaveSettings("SelectedLocalNetworkAddress", value); }
        }

        public static string SelectedServerNetworkAddress
        {
            get { return StorageExpert.GetSetting("SelectedServerNetworkAddress"); }
            set { StorageExpert.SaveSettings("SelectedServerNetworkAddress", value); }
        }

        public static string SelectedConnectionTypeString
        {
            get { return StorageExpert.GetSetting("SelectedConnectionType"); }
            set { StorageExpert.SaveSettings("SelectedConnectionType", value); }
        }

        public static IEnumerable<string> NetworkAddressHistory
        {
            get
            {
                var stored = StorageExpert.GetSetting("NetworkAddressHistory");
                return (stored == null) ? null : stored.Split(',');
            }
            set { StorageExpert.SaveSettings("NetworkAddressHistory", StringExpert.CommaSeparate(value)); }
        }

        public static EventLevel AppLogDebugWriterEventLevel
        {
            get { return EventLevel.LogAlways; }
        }

        //public static Rect? InputSpace
        //{
        //    get {
        //        string serialized = StorageExpert.GetSetting("InputDimension");
        //        if (serialized == null)
        //            return null;
        //        List<double> tokens = serialized.Split(',').Select(s => double.Parse(s)).ToList();

        //        return new Rect(tokens[0], tokens[1], tokens[2], tokens[3]);
        //    }
        //    set {
        //        var r = value.Value;
        //        string serialized = StringExpert.CommaSeparate(new object[] { r.Left, r.Top, r.Width, r.Height });
        //        StorageExpert.SaveSettings("InputDimension",  serialized);
        //    }
        //}
        public static InputSpace InputSpace
        {
            get
            {
                if (inputSpace == null)
                {
                    string serialized = StorageExpert.GetSetting("InputSpace12345");
                    if (serialized != null)
                        inputSpace = StringExpert.FromJson<InputSpace>(serialized);
                }

                return inputSpace;
            }
            set
            {
                inputSpace = value;

                string json = StringExpert.ToJson(value);
                StorageExpert.SaveSettings("InputSpace12345", json);
            }
        }

        public static string MarkerName
        {
            get { return StorageExpert.GetSetting("MarkerName"); }
            set { StorageExpert.SaveSettings("MarkerName", value); }
        }

        public static string DistanceMarkerName
        {
            get { return StorageExpert.GetSetting("DistanceMarkerName"); }
            set { StorageExpert.SaveSettings("DistanceMarkerName", value); }
        }

        public static int DistanceUpperLimit
        {
            get { return int.Parse(StorageExpert.GetSetting("DistanceUpperLimit") ?? "100"); }
            set { StorageExpert.SaveSettings("DistanceUpperLimit", value.ToString()); }
        }

        public static int DistanceMeasureRidigBodyIDReference
        {
            get { return int.Parse(StorageExpert.GetSetting("DistanceMeasureRidigBodyIDReference") ?? "0"); }
            set { StorageExpert.SaveSettings("DistanceMeasureRidigBodyIDReference", value.ToString()); }
        }

        public static bool CalculateRidigBodyStats
        {
            get { return bool.Parse(StorageExpert.GetSetting("CalculateRidigBodyStats") ?? bool.FalseString); }
            set { StorageExpert.SaveSettings("CalculateRidigBodyStats", value.ToString()); }
        }

        public static int PointerMarkerID
        {
            get { return int.Parse(StorageExpert.GetSetting("PointerMarkerID") ?? "1"); }
            set { StorageExpert.SaveSettings("PointerMarkerID", value.ToString()); }
        }

        public static double SmoothingBase
        {
            get { return double.Parse(StorageExpert.GetSetting("SmoothingBase") ?? "1.02337"); }
            set { StorageExpert.SaveSettings("SmoothingBase", value.ToString()); }
        }

        public static double SmoothingCutoffMilliSeconds
        {
            get { return double.Parse(StorageExpert.GetSetting("SmoothingCutoffMilliSeconds") ?? "100"); }
            set { StorageExpert.SaveSettings("SmoothingCutoffMilliSeconds", value.ToString()); }
        }

        public static double SmoothingDeltaMultiplier
        {
            get { return double.Parse(StorageExpert.GetSetting("SmoothingDeltaMultiplier") ?? "0.0001"); }
            set { StorageExpert.SaveSettings("SmoothingDeltaMultiplier", value.ToString()); }
        }

        //public static PlanePoint DirectionalOffset
        //{
        //    get {
        //        string str = StorageExpert.GetSetting("DirectionalOffset");

        //        return string.IsNullOrEmpty(str) ? new XYPoint() : StringExpert.FromJson<XYPoint>(str);
        //    }
        //    set { StorageExpert.SaveSettings("DirectionalOffset", value.ToString()); }
        //}

        public static double DirectionalOffsetX
        {
            get { return double.Parse(StorageExpert.GetSetting("DirectionalOffsetX") ?? "0"); }
            set { StorageExpert.SaveSettings("DirectionalOffsetX", value.ToString()); }
        }

        public static double DirectionalOffsetY
        {
            get { return double.Parse(StorageExpert.GetSetting("DirectionalOffsetY") ?? "0"); }
            set { StorageExpert.SaveSettings("DirectionalOffsetY", value.ToString()); }
        }

        public static double DirectionalAmplification
        {
            get { return double.Parse(StorageExpert.GetSetting("DirectionalAmplification") ?? "1"); }
            set { StorageExpert.SaveSettings("DirectionalAmplification", value.ToString()); }
        }

        public static MotionTrackingSource MotionTrackingSource
        {
            get
            {
                return (MotionTrackingSource)Enum.Parse(
                    typeof(MotionTrackingSource),
                    StorageExpert.GetSetting("MotionTrackingSource") ?? MotionTrackingSource.Kinect.ToString()
                    );
            }
            set { StorageExpert.SaveSettings("MotionTrackingSource", value.ToString()); }
        }

        public static JointType KinectJointTypePointer
        {
            get
            {
                return EnumGetOrDefault<JointType>("KinectJointTypePointer", JointType.HandTipRight);
            }
            set { StorageExpert.SaveSettings("KinectJointTypePointer", value.ToString()); }
        }


        private static T EnumGetOrDefault<T>(string key, T def)
        {
            string serialized = StorageExpert.GetSetting(key);

            return (serialized == null || string.IsNullOrEmpty(serialized)) ? def : (T)Enum.Parse(typeof(T), serialized);
        }

        public static Exp3PointMapper Exp3PointRemapper
        {
            get
            {
                if (exp3PCAData != null)
                    return exp3PCAData;

                log.Verbose("Deserializing exp3 point remapper..");

                string json = StorageExpert.GetSetting("Exp3PCAData4");
                var value = string.IsNullOrEmpty(json) ? null : StringExpert.FromJson<Exp3PointMapper>(json);

                return value;
            }
            set
            {
                StorageExpert.SaveSettings("Exp3PCAData4", StringExpert.ToJson(value));
                exp3PCAData = value;
            }
        }

        #endregion
    }
}


