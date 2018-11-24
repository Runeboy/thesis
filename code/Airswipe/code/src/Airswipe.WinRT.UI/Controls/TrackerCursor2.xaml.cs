using Airswipe.WinRT.Core;
using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.UI.Common;
using Windows.UI.Xaml.Controls;


namespace Airswipe.WinRT.UI.Controls
{
    public sealed partial class TrackerCursor2 : UserControl
    {
        #region

        public const int SIZE = 20;

        #endregion
        #region Constructor

        public TrackerCursor2()
        {
            Loaded += TrackerCursor_Loaded; ;

            InputSpace = AppSettings.InputSpace;
        }

        #endregion
        #region Event handlers

        private void TrackerCursor_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            AppMotionTrackerClient.Instance.TrackedPointReady += Instance_TrackedPointReady;
        }

        private void Instance_TrackedPointReady(OffscreenPoint[] t)
        {
            if (t.Length == 0)
                return;

            OffscreenPoint point = t[0];

            //InputSpace.
        }

        #endregion
        #region Properties

        private InputSpace InputSpace { get; set; }

        #endregion
    }
}
