using Airswipe.WinRT.Core.Data.Dto;
using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.MotionTracking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.Foundation;

namespace Airswipe.WinRT.Core.Data
{
    [DuplicateTrial(TrialModeFilter = TrialMode.Horisontal, TrialGroupNameAppendToProjectionMode = "-DimsConcatenation")]
    [DuplicateTrial(TrialModeFilter = TrialMode.Vertical, TrialGroupNameAppendToProjectionMode = "-DimsConcatenation")]
    public class Trial : JsonObject
    {

        #region Fields

        [JsonIgnore]
        private static ILogger log = new TypeLogger<Trial>();

        public Guid UID = Guid.NewGuid();
        private Point fromTargetToReleasePosition;
        private string groupName;
        private List<PropertyInfo> outlierProps = new List<PropertyInfo>();

        //private TrialMode mode = TrialMode.Horisontal;

        #endregion
        #region Constructor 

        public Trial(string groupID = "ungrouped")
        {
            CreateTime = DateTime.Now;
            Mode = TrialMode.Horisontal;
            Comment = "";
            ReleaseValues = new List<double>();

            //GroupName = Name;
        }

        public void NotifySpaceReleased(double releaseValue)
        {
            //var releaseValue = ReleasePositionValue;
            if ((releaseValue != 0) && (releaseValue != TargetValue) && !ReleaseValues.Contains(releaseValue))
                ReleaseValues.Add(ReleasePositionValue);
        }


        public void End(bool isForcefullyEnded)
        {
            EndTime = DateTime.Now;
            //FromTargetToReleasePosition = fromTargetToReleasePosition;
            IsForcefullyEnded = isForcefullyEnded;
        }

        public void Restart()
        {
            log.Verbose("trial started");

            StartTime = DateTime.Now;
            EndTime = default(DateTime);

            IsForcefullyEnded = false;

            //FromOrigoToTargetPosition = default(Point);

            ReleaseValues.Clear();
            FromTargetToReleasePosition = default(Point);
            TouchCount = 0;
            InertialDistanceTraveled = 0;
            NoninertialDistanceTraveled = 0;
            OvershootTravelDistance = 0;
        }
        //public Point TargetPosition { get; set; }

        #endregion
        #region Properties

        public string Name
        {
            get
            {
                return string.Format(
                    "{0}-{1}{2}",
                    ProjectionMode,
                    Mode,
                    (MoveBullsEyeInsteadOfMap ? "-Estimation" : "")
                    );
            }
        }

        //[CsvIgnore]
        //public string ExtendedName
        //{
        //    get { return "Target(" + TargetValue + ")-" + Name; }
        //}

        public string GroupName
        {
            get { return string.IsNullOrEmpty(groupName) ? Name : groupName; }
            set { groupName = value; }
        }



        [JsonConverter(typeof(StringEnumConverter))]
        public ProjectionMode ProjectionMode { get; set; }

        [CsvIgnore]
        public DateTime CreateTime { get; set; }

        [CsvIgnore]
        public DateTime StartTime { get; set; }

        [CsvIgnore]
        public DateTime EndTime { get; set; }

        //[CsvIngore]
        public bool IsCompleted { get { return EndTime != default(DateTime); } }

        [CsvIgnore]
        public TimeSpan Duration
        {
            get
            {
                if (StartTime == default(DateTime))
                    return default(TimeSpan);

                return (EndTime == default(DateTime) ? DateTime.Now : EndTime) - StartTime;
            }
        }

        [CsvDoStats]
        [OutlierLimit(25)]
        public double DurationSeconds
        {
            get { return Duration.TotalSeconds; }
        }

        public bool MoveBullsEyeInsteadOfMap { get; set; }

        //For 2D mode
        //[CsvIgnore]
        public double UIAngle { get; set; }

        public double UIAngleDegrees { get { return GeometryExpert.RadianToDegree(UIAngle); } }

        [JsonConverter(typeof(StringEnumConverter))]
        [CsvIgnore]
        public TrialMode Mode { get; set; }

        [CsvIsIndex]
        public double TargetValue { get; set; }

        [CsvIgnore]
        public Point FromOrigoToTargetPosition { get; set; }

        public double FromOrigoToTargetLength
        {
            get { return GeometryExpert.Euclidean(FromOrigoToTargetPosition); }
        }

        public double FromOrigoToTargetPositionX { get { return FromOrigoToTargetPosition.X; } }

        public double FromOrigoToTargetPositionY { get { return FromOrigoToTargetPosition.Y; } }

        [CsvIgnore]
        public Point FromTargetToReleasePosition
        {
            get { return fromTargetToReleasePosition; }
            set
            {
                fromTargetToReleasePosition = value;
            }
        }

        [CsvDoStats(Confidence = false)]//////////////////
        //[CsvIgnore]
        public double FromTargetToReleaseLength
        {
            get { return GeometryExpert.Euclidean(FromTargetToReleasePosition); }
        }

        [CsvDoStats(Confidence = true)]
        public double FromTargetToReleaseVerticalLength
        {
            get { return FromTargetToReleaseLength * (TargetValue < 0 ? -1 : 1); }
        }

        //[CsvDoStats]
        [CsvIgnore]
        public double TargetDistanceFromOrigo
        {
            get { return GeometryExpert.Euclidean(FromOrigoToTargetLength); }
        }

        //[CsvDoStats]
        public double TargetHorizontalDistanceFromOrigo
        {
            get { return TargetDistanceFromOrigo * (TargetValue < 0 ? -1 : 1); }
        }

        //[CsvDoStats]
        public double FromTargetToReleasePositionX
        {
            get { return fromTargetToReleasePosition.X; }
        }

        //[CsvDoStats]
        public double FromTargetToReleasePositionY
        {
            get { return fromTargetToReleasePosition.Y; }
        }

        [CsvIgnore]
        public bool IsStarted { get { return StartTime != default(DateTime); } }

        [CsvIgnore]
        public bool IsOffscreenSpaceEnabled { get; set; }

        [CsvIgnore]
        public bool IsIntertialMoveAllowedOnscreen { get { return !IsOffscreenSpaceEnabled; } }

        [CsvIgnore]
        public string Comment { get; set; }

        [CsvIgnore]
        public bool IsForcefullyEnded { get; private set; }

        //public bool IsMapMoveEnabled
        //{
        //    get { return !IsValuesToBeEstimated; }
        //}


        [CsvIgnore]
        private double UnitsPerTick
        {
            get { return (TargetValue == 0) ? 0 : GeometryExpert.Euclidean(FromOrigoToTargetPosition.X, FromOrigoToTargetPosition.Y) / Math.Abs(TargetValue); }
        }


        [CsvIgnore]
        public Point FromOrigoToReleasePosition
        {
            get
            {
                return new Point(
                    FromOrigoToTargetPosition.X + FromTargetToReleasePosition.X,
                    FromOrigoToTargetPosition.Y + FromTargetToReleasePosition.Y
                );
            }
        }

        [CsvDoStats(Confidence = false)]
        [CsvPcaX]
        public double ReleasePositionX { get { return FromOrigoToReleasePosition.X; } }

        [CsvDoStats(Confidence = false)]
        [CsvPcaY]
        public double ReleasePositionY { get { return FromOrigoToReleasePosition.Y; } }


        [CsvIgnore]
        public double ReleasePositionValue
        {
            get
            {
                if (UnitsPerTick == 0)
                    return 0;

                switch (Mode)
                {
                    case TrialMode.Horisontal: return FromOrigoToReleasePosition.X / UnitsPerTick;
                    case TrialMode.Vertical: return -1 * FromOrigoToReleasePosition.Y / UnitsPerTick; // negative because the canvas y values are opposite of IU
                    case TrialMode.TwoDimensional:
                        bool isReleaseUpperHalf = (FromOrigoToReleasePosition.Y < 0);
                        return (isReleaseUpperHalf ? 1 : -1) * GeometryExpert.Euclidean(FromOrigoToReleasePosition.X, FromOrigoToReleasePosition.Y) / UnitsPerTick;
                }

                throw new NotImplementedException("No case handler for mode.");
                //return new Point(
                //    FromOrigoToReleasePosition.X / UnitsPerTick,
                //    FromOrigoToReleasePosition.Y / UnitsPerTick
                //    );
            }
        }

        //[CsvIgnore]
        //public PlanePoint FromOrigoToExp3TargetPosition { get; set;} 

        //[CsvIgnore]
        //public double FromExp3TargetToReleaseLength
        //{
        //    get
        //    {
        //        return GeometryExpert.Euclidean(
        //            FromOrigoToExp3TargetPosition.Subtract(XYPoint.FromPoint(FromOrigoToReleasePosition))
        //            );
        //    }
        //}

        //[CsvDoStats(Confidence = true)]
        //public double FromExp3TargetToReleaseVerticalLength
        //{
        //    get { return FromExp3TargetToReleaseLength * (TargetValue < 0 ? -1 : 1); }
        //}

        [CsvIgnore]
        public PlanePoint FromOrigoToExp3ReleasePosition { get; set; }

        public double FromOrigoToExp3ReleasePositionX { get { return (FromOrigoToExp3ReleasePosition == null) ? 0 : FromOrigoToExp3ReleasePosition.X; } }

        public double FromOrigoToExp3ReleasePositionY { get { return (FromOrigoToExp3ReleasePosition == null) ? 0 : FromOrigoToExp3ReleasePosition.Y; } }

        [CsvIgnore]
        public double FromTargetToExp3ReleaseLength
        {
            get
            {
                if (FromOrigoToExp3ReleasePosition == null)
                    return 0;

                return GeometryExpert.Euclidean(
                    FromOrigoToExp3ReleasePosition.Subtract(XYPoint.FromPoint(FromOrigoToTargetPosition))
                    );
            }
        }

        [CsvDoStats(Confidence = true)]
        public double FromTargetToExp3ReleaseVerticalLength
        {
            get { return FromTargetToExp3ReleaseLength * (TargetValue < 0 ? -1 : 1); }
        }



        [CsvDoStats]
        public double ReleasePositionTargetDiff
        {
            get { return ReleasePositionValue - TargetValue; }
        }

        [CsvDoStats]
        public double ReleasePositionAbsTargetDiff
        {
            get { return Math.Abs(ReleasePositionValue - TargetValue); }
        }
        //public double ReleasePositionEuclideanValue
        //{
        //    get
        //    {
        //        if (UnitsPerTick == 0)
        //            return 0;

        //        return GeometryExpert.Euclidean(FromOrigoToReleasePosition.X, FromOrigoToReleasePosition.Y) / UnitsPerTick;
        //    }
        //}

        [CsvIgnore]
        public double InertialDistanceTraveled { get; set; }

        [CsvIgnore]
        public double NoninertialDistanceTraveled { get; set; }

        public double TotalDistanceTraveled
        {
            get { return (InertialDistanceTraveled + NoninertialDistanceTraveled); }
        }

        [CsvDoStats(Confidence = false)]
        public double InertialDistanceTraveledRatio
        {
            get { return (TotalDistanceTraveled == 0) ? 0 : InertialDistanceTraveled / TotalDistanceTraveled; }
        }


        //public double FinalProjectionX
        //{
        //    get { return (FinalOffscreenLocation == null) ? 0 : FinalOffscreenLocation.X; }
        //}

        //public double FinalProjectionY
        //{
        //    get { return (FinalOffscreenLocation == null) ? 0 : FinalOffscreenLocation.Y; }
        //}


        public bool IsOutlier { get; set; }

        public int BullsEyeEntryCount { get; set; }

        [CsvIgnore]
        public double OvershootTravelDistance { get; set; }

        [CsvDoStats(Confidence = false)]
        public double OvershootTravelRatio
        {
            get { return (TotalDistanceTraveled == 0) ? 0 : OvershootTravelDistance / TotalDistanceTraveled; }
        }

        [CsvDoStats(Confidence = false)]
        public double OvershootTravelRatioPercentage { get { return OvershootTravelRatio * 100; } }

        [CsvDoStats(Confidence = false)]
        public int TouchCount { get; set; }

        [CsvIgnore] // omits onscreen releases for now
        public List<double> ReleaseValues { get; }

        public bool IsStatistic { get; set; }

        public bool IsDuplication { get; set; }

        public double FinalOffscreenX
        {
            get { return (FinalOffscreenLocation == null) ? 0 : FinalOffscreenLocation.X; }
        }

        public double FinalOffscreenY
        {
            get { return (FinalOffscreenLocation == null) ? 0 : FinalOffscreenLocation.Y; }
        }

        public double FinalOffscreenZ
        {
            get { return (FinalOffscreenLocation == null) ? 0 : FinalOffscreenLocation.Z; }
        }

        [CsvIgnore] // omits onscreen releases for now
        public SpatialPoint FinalOffscreenLocation { get; set; }


        [CsvIgnore] // omits onscreen releases for now
        public SpatialPoint FinalOffscreenLocationWRTDevice { get; set; }


        public double FinalOffscreenLocationWRTDeviceX
        {
            get { return (FinalOffscreenLocationWRTDevice == null) ? 0 : FinalOffscreenLocationWRTDevice.X; }
        }

        public double FinalOffscreenLocationWRTDeviceY
        {
            get { return (FinalOffscreenLocationWRTDevice == null) ? 0 : FinalOffscreenLocationWRTDevice.Y; }
        }

        public double FinalOffscreenLocationWRTDeviceZ
        {
            get { return (FinalOffscreenLocationWRTDevice == null) ? 0 : FinalOffscreenLocationWRTDevice.Z; }
        }



        [CsvIgnore]
        [JsonIgnore]
        public List<PropertyInfo> OutlierProps { get { return outlierProps; } }

        //[CsvIgnore] 
        //[JsonIgnore]
        //public string OutlierPropNames {
        //    get {
        //        return StringExpert.CommaSeparate(outlierProps.Select(p => p.Name));
        //    }
        //}

        #endregion


        public bool IsExp3SpaceToBeUsed { get; set; }
    }
}
