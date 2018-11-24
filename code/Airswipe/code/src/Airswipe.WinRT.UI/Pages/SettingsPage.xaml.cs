using Airswipe.WinRT.Core;
using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.UI.Common;
using WindowsPreview.Kinect;

namespace Airswipe.WinRT.UI.Pages
{
    public sealed partial class SettingsPage : BasicPage
    {
        #region Fields

        private ILogger log = new TypeLogger<SettingsPage>();

        #endregion
        #region Constructors 

        public SettingsPage()
        {
            InitializeComponent();
        }

        #endregion
        #region Properties

        public bool AutoConnectOnStartup
        {
            get { return AppSettings.AutoConnectOnStartup; }
            set { AppSettings.AutoConnectOnStartup = value; }
        }

        //public string MarkerName
        //{
        //    get { return AppSettings.MarkerName; }
        //    set { AppSettings.MarkerName = value; }
        //}

        public string PointerMarkerID
        {
            get { return AppSettings.PointerMarkerID.ToString(); }
            set { AppSettings.PointerMarkerID = int.Parse(value); }
        }

        public string MotionTrackingSourceType { get { return ""; } } // typeof(MotionTrackingSource).FullName; } }

        public JointType KinectJointTypePointer
        {
            get { return AppSettings.KinectJointTypePointer;  }
            set
            {
                AppSettings.KinectJointTypePointer = value;
            }
        }

        #endregion

        private void DirectionalAmplification_ValueChange(double newValue)
        {
            AppSettings.DirectionalAmplification = newValue;
        }

        public double DirectionalAmplification { get { return AppSettings.DirectionalAmplification; } }



        private void SmoothingCutoffDoubleBox_ValueChange(double newValue)
        {
            SmoothingCutoffMilliSeconds = newValue;
        }

        private void DeltaMultiplerDoubleBox_ValueChange(double newValue)
        {
            SmoothingDeltaMultiplier = newValue;
        }

        private void OffsetX_ValueChange(double newValue)
        {
            AppSettings.DirectionalOffsetX = newValue;
        }

        private void OffsetY_ValueChange(double newValue)
        {
            AppSettings.DirectionalOffsetY = newValue;
        }

        public double DirOffsetX { get { return AppSettings.DirectionalOffsetX; } }
        public double DirOffsetY { get { return AppSettings.DirectionalOffsetY; } }

        public double SmoothingCutoffMilliSeconds
        {
            get { return AppSettings.SmoothingCutoffMilliSeconds; }
            set
            {
                AppSettings.SmoothingCutoffMilliSeconds = value;
                AppMotionTrackerClient.Instance.SmoothingCutoffMilliSeconds = value;

                log.Info("Smoothing cutoff (ms) changed to: " + value);
            }
        }

        public double SmoothingBase
        {
            get { return AppSettings.SmoothingBase; }
            set
            {
                AppSettings.SmoothingBase = value;
                AppMotionTrackerClient.Instance.SmoothingBase = value;

                log.Info("Smoothing base changed to: " + value);
            }
        }

        public double SmoothingDeltaMultiplier
        {
            get { return AppSettings.SmoothingDeltaMultiplier; }
            set
            {
                AppSettings.SmoothingDeltaMultiplier = value;
                AppMotionTrackerClient.Instance.SmoothingDeltaMultiplier = value;

                log.Info("Smoothing Delta Multiplier changed to: " + value);
            }
        }


        private void SmoothingBaseDoubleBox_ValueChange(double newValue)
        {
            SmoothingBase = newValue;
        }

    }

}
