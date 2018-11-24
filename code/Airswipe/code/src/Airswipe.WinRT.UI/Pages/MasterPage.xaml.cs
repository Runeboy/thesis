using Airswipe.WinRT.Core;
using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.UI.Common;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Airswipe.WinRT.UI.Pages
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MasterPage : EdgeTapPage
    {
        #region Fields

        private static ILogger log = new TypeLogger<MasterPage>();
        private const bool IsSideBarIndicatorUsageEnabled = false;

        //Windows.UI.Input.GestureRecognizer gr = new Windows.UI.Input.GestureRecognizer();  
        //public bool IsSideBarIndicatorEnabled { get; set; }

        public Thickness SidebarIndicatorMargin { get { return new Thickness(0, 0, 5, 0); } }

        public delegate void ProjectionCursorVisibilityRequestHandler(bool isVisible);
        public static event ProjectionCursorVisibilityRequestHandler ProjectionCursorVisibilityRequest;

        public delegate void ProjectionCursorVisibilityToggleRequestHandler();
        public static event ProjectionCursorVisibilityToggleRequestHandler ProjectionCursorVisibilityToggleRequest;

        public delegate void OffscreenReleaseByKeyDownHandler();
        public static event OffscreenReleaseByKeyDownHandler OffscreenReleaseByKeyDown;

        #endregion
        #region Constructors

        public MasterPage()
        {
            InitializeComponent();


            ListenForOffscreenTapReleaseKey();

            IsRightTapEnabled = true;
            //IsSideBarIndicatorEnabled = true;

            Loaded += (object sender, RoutedEventArgs e) =>
            {
                ShowOffscreenTrackerOnPointsReceived();
            };

            Sidebar.HideRequest += (sender, args) => SetConnectivitySidebarVisiblity(false);
            ListenForCursorVisibilityRequest();

            EdgeTap += (location) =>
            {
                if (location == RectLineBoundary.Right)
                    SetConnectivitySidebarVisiblity(true);
            };

            //Popup.IsOpen = true;
            //SetConnectivitySidebarVisiblity(true);

            NotifySidebarOnPopupDisplayStateChange();
        }

        private void ListenForCursorVisibilityRequest()
        {
            ProjectionCursorVisibilityRequest += (isVisible) =>
            {
                Visibility v = isVisible ? Visibility.Visible : Visibility.Collapsed;

                SetCursorVisibilites(v);
            };

            ProjectionCursorVisibilityToggleRequest += () =>
            {
                SetCursorVisibilites(TrackerPositionEllipse.Visibility == Visibility.Collapsed? Visibility.Visible   : Visibility.Collapsed);
            };
        }

        private void SetCursorVisibilites(Visibility v)
        {
            TrackerPositionEllipse.Visibility = v;
            DirectionalTrackedPositionEllipse.Visibility = v;
            SphericalTrackedPositionEllipse.Visibility = v;
        }

        private void ListenForOffscreenTapReleaseKey()
        {
            KeyDown += (object sender, KeyRoutedEventArgs e) => {
                if (
                    e.Key == VirtualKey.Control ||
                    e.Key == VirtualKey.Escape ||
                    //e.Key == VirtualKey.Enter ||
                    e.Key == VirtualKey.Space 
                    )
                {
                    log.Info("Offscreen release by key-down");
                    if (OffscreenReleaseByKeyDown != null)
                        OffscreenReleaseByKeyDown();
                }
            };
        }

        public static void RequestCursorVisibility(bool b)
        {
            if (ProjectionCursorVisibilityRequest != null)
                ProjectionCursorVisibilityRequest(b);

        }
        public static void RequestCursorToggle()
        {
            if (ProjectionCursorVisibilityToggleRequest != null)
                ProjectionCursorVisibilityToggleRequest();

        }

        private void ShowOffscreenTrackerOnPointsReceived()
        {
            //AppTrackerPointProjector.Instance.PlaneNormalTrackingPointProjected += (projection) =>
            //{
            //    SetTrackingCursorPosition(projection, TrackerPositionEllipse);
            //};
            //AppTrackerPointProjector.Instance.DirectionalTrackingPointProjected += (projection) =>
            //{
            //    SetTrackingCursorPosition(projection, DirectionalTrackedPositionEllipse);
            //};

            AppTrackerPointProjector.Instance.TrackingPointProjected += (projection) =>
            {
                SetTrackingCursorPosition(
                    projection,
                    GetProjectinoEllipse(projection)
                    );
            };

            //AppMotionTrackerClient.Instance.TrackedPointReady += (OffscreenPoint[] t) =>
            //{
            //    if (t.Length == 0)
            //        return;

            //    OffscreenPoint point = t[t.Length - 1]; // TODO: possibly use last if OptiTrack is the tracker?

            //    if (AppSettings.InputSpace == null)
            //        return;

            //    ProjectedXYPoint displayPosition = AppSettings.InputSpace.MapOffscreenToOnscreen(point);

            //    //log.Verbose("Tracking offscreen marker: ({0}:{1}), distance: {2}", displayPosition.X, displayPosition.Y, displayPosition.ProjectionDistance);
            //    SetTrackingCursorPosition(displayPosition);

            //    //SpatialPlane screenPlane = AppSettings.InputSpace.Offscreen;
            //    //ProjectedSpacialPoint screenProjection = point.Project(screenPlane);

            //    //InputSpace.
            //};
        }

        private Ellipse GetProjectinoEllipse(WinRT.Core.Data.Dto.ProjectedXYPoint projection)
        {
            switch(projection.Mode)
            {
                case ProjectionMode.PlaneNormal: return TrackerPositionEllipse;
                case ProjectionMode.Directional: return DirectionalTrackedPositionEllipse;
                case ProjectionMode.Spherical: return SphericalTrackedPositionEllipse;
            }

            throw new System.Exception("Cannot determine projection ellipse");
        }

        private void SetTrackingCursorPosition(PlanePoint point, Shape cursorControl)
        {

            //TrackerPositionGrid.SetValue(Canvas.LeftProperty, point.X - TrackerPositionEllipse.Width / 2);
            //TrackerPositionGrid.SetValue(Canvas.TopProperty, point.Y - TrackerPositionEllipse.Height / 2);
            cursorControl.SetValue(Canvas.LeftProperty, point.X - cursorControl.Width / 2);
            cursorControl.SetValue(Canvas.TopProperty, point.Y - cursorControl.Height / 2);

            
            //ProjectionDistanceTextBlock.SetValue(Canvas.LeftProperty, point.X - ProjectionDistanceTextBlock.ActualWidth / 2);
            //ProjectionDistanceTextBlock.SetValue(Canvas.TopProperty, point.Y - ProjectionDistanceTextBlock.ActualHeight / 2);
        }

        private void NotifySidebarOnPopupDisplayStateChange()
        {
            Popup.Closed += (object sender, object e) =>
            {
                log.Verbose("Sidebar closed");
                Sidebar.NotifiedContainerVisibilityChanged(false);
            };
            Popup.Opened += (object sender, object e) =>
            {
                log.Verbose("Sidebar opened");
                Sidebar.NotifiedContainerVisibilityChanged(true);
            };
        }

        private void ToggleSideBarOnRightClick()
        {
            RightTapped += (object sender, RightTappedRoutedEventArgs e) =>
            {
                //IsSideBarIndicatorEnabled = !IsSideBarIndicatorEnabled;

                //log.Info("is side bar indicator enabled: " + IsSideBarIndicatorEnabled);

                SetConnectivitySidebarVisiblity(true);
            };
        }

        private void SetConnectivitySidebarVisiblity(bool isVisible)
        {
            Popup.IsOpen = isVisible;
            Sidebar.NotifyVisibilityChange(isVisible);

            //Popup.Margin = new Thickness();
            //SetElementRightMargin(Popup, Sidebar.ActualWidth);

            //if (isVisible)
            //{
            //    SetElementRightMargin(Popup, Sidebar.ActualWidth);
            //    Popup.IsOpen = true;
            //}
            //else
            //{
            //    if (!IsSideBarIndicatorUsageEnabled)
            //        //Popup.IsOpen = false;
            //        Popup.IsOpen = false;
            //    else
            //        if (IsSideBarIndicatorEnabled)
            //    {
            //        Popup.Margin = SidebarIndicatorMargin;
            //        Popup.IsOpen = true;
            //    }
            //    else
            //        Popup.Margin = new Thickness();
            //    //Popup.IsOpen = false;
            //}
        }

        private static void SetElementRightMargin(FrameworkElement e, double value)
        {
            Thickness margin = e.Margin;
            margin.Right = value;
            e.Margin = margin;
        }

        ///// <summary>
        ///// Show and hide a task pane or other large UI container from the edge of the screen.
        ///// http://msdn.microsoft.com/en-US/library/windows/apps/hh975420
        ///// </summary>
        //private void ShowTaskPane()
        //{
        //    SetElementRightMargin(Popup, Sidebar.ActualWidth);

        //    Popup.IsOpen = true;
        //}

        #endregion
        #region Properties

        //private Popup Popup
        //{
        //    get
        //    {
        //        if (popup == null)
        //        {
        //            //  var rect = new SidebarControl //new Rectangle
        //            //  {
        //            //      //Fill = new SolidColorBrush(Colors.Red),
        //            ////      Width = width,
        //            //  };

        //            popup = new Popup
        //            {
        //                //IsLightDismissEnabled = true,
        //                ChildTransitions = new TransitionCollection(),
        //                Child = Sidebar,
        //            };

        //            popup.ChildTransitions.Add(new PaneThemeTransition { Edge = EdgeTransitionLocation.Right });

        //            (popup.Child as FrameworkElement).Height = this.ActualHeight;

        //            (popup.Child as FrameworkElement).Tapped += (sender, args) => log.Info("************************************************" + args.Handled); // popup.IsOpen = false;

        //            popup.SetValue(Canvas.LeftProperty, this.ActualWidth - 10);
        //            popup.IsOpen = true;
        //        }

        //        return popup;
        //    }
        //}

        //private SidebarControl Sidebar
        //{
        //    get
        //    {
        //        if (sidebar == null)
        //            sidebar = new SidebarControl();

        //        return sidebar;
        //    }
        //}

        #endregion


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        public Frame ContentFrame
        {
            get { return this.LayoutContentFrame; }
        }

        private void Sidebar_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            //SetConnectivitySidebarVisiblity(false);
            SetConnectivitySidebarVisiblity(false);
        }

        private void TrackerPositionEllipse_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = false;
        }
    }
}
