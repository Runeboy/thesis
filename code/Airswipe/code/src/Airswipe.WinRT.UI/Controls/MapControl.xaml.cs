using Airswipe.WinRT.Core.Log;
using System;
using System.Linq;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Airswipe.WinRT.UI.Common;
using Airswipe.WinRT.Core.Data.Dto;
using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core;
using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.UI.Pages;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Core;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Airswipe.WinRT.UI.Controls
{
    public sealed partial class MapControl : UserControl
    {
        #region Fields

        public static readonly Color TICK_POSITIVE_COLOR = Colors.OrangeRed;
        public static readonly Color TICK_NEGATIVE_COLOR = Colors.DarkBlue;

        public static readonly Color TICK_TARGET_COLOR = Colors.LimeGreen;

        public static readonly Color RING_COLOR = Colors.Gray;

        public static readonly Color BULLEYE_TARGET_COLOR = Colors.Green;
        public static readonly Color BULLEYE_NONTARGET_COLOR = Colors.Red;

        public static readonly Color TICK_FONT_COLOR = Colors.White;

        private static readonly TimeSpan TARGET_HOLD_TIME = TimeSpan.FromSeconds(3);

        private Point lastMapContainerTouchPoint;

        private const int MAP_OFFSCREEN_TRANSITION_PIXEL_COUNT_BUFFER = 20; // distance from screen which is perceived as transition (since we do not necesarily get point exactly on onscreen edge)

        ILogger log = new TypeLogger<MapControl>();

        public enum MapMoveStatuses { NotMoving, MoveByOnScreen, MoveByOffScreen }

        private MapMoveStatuses mapMoveStatus = MapMoveStatuses.NotMoving;



        //Rect mapRect;
        //Rect mapContainerRect;



        private static readonly Dictionary<MapMoveStatuses, Color> MapCanvasMoveBackgrounds = new Dictionary<MapMoveStatuses, Color> {
                { MapMoveStatuses.NotMoving, Colors.White },
                { MapMoveStatuses.MoveByOnScreen, Colors.FloralWhite},
                { MapMoveStatuses.MoveByOffScreen, Colors.Cornsilk},
            };


        private delegate void TargetPositionChange(Point newPosition);
        private event TargetPositionChange TargetPositionChanged;

        public delegate void UIChangeHandler();
        public event UIChangeHandler UIChange;


        public delegate void TargetHoldAchievement();
        public event TargetHoldAchievement TargetHoldAchieved;

        public delegate void ProjectionReleaseHandler();
        public event ProjectionReleaseHandler ProjectionRelease;

        public delegate void ScreenReleaseHandler();
        public event ScreenReleaseHandler ScreenRelease;

        private DispatcherTimer targetTimer = new DispatcherTimer { Interval = TARGET_HOLD_TIME };

        //public delegate void TargetEnteredHandler();
        //public event TargetEnteredHandler TargetEntered;
        public int BullsEyeEntryCount { get; set; }


        #endregion
        #region Constructor

        public MapControl()
        {
            InitializeComponent();

            IsModeModeColoringEnabled = true;


            SetMapCanvasBackgroundByMoveStatus();

            Loaded += (s, a) =>
            {
                InitializeBullsEye();
            };

            SetupTargetTimer();

            StopMapMoveOnOffscreenTapReleaseKeyDown();

            AppTrackerPointProjector.Instance.TrackingPointProjected += Instance_TrackedPointProjected;
        }

        private void StopMapMoveOnOffscreenTapReleaseKeyDown()
        {
            MasterPage.OffscreenReleaseByKeyDown += () =>
            {
                //MapMoveStatus = MapMoveStatuses.NotMoving;
                HandleOffscreenRelease();
            };
        }

        private void Instance_TrackedPointProjected(ProjectedXYPoint projection)
        {
            bool isProjectionModeUsedByTrial = (projection.Mode == MapMoveProjectionMode);



            if (MapMoveStatus == MapMoveStatuses.MoveByOffScreen && isProjectionModeUsedByTrial)
            {
                //Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                //    () =>
                //    {
                //        MapCanvas.Background = new SolidColorBrush(Colors.LightYellow);
                //    }
                //    );


                bool canMove = (LastOffscreenProjection != null);

                if (ProjectionPreProcess != null)
                {
                    ProjectionPreProcess(projection);

                    if (LastOffscreenProjection != null)
                        projection.Delta = projection.Subtract(LastOffscreenProjection);
                }
                LastOffscreenProjection = projection;


                bool isOffscreenTapRelease = (Math.Abs(projection.ProjectionDistance) >= AppSettings.OFFSCREEN_TAP_RELEASE_PROJECTION_DISTANCE_MILLIMETER);
                if (isOffscreenTapRelease)
                {
                    HandleOffscreenRelease();
                }
                else {
                    if (canMove)
                    PerformMove(
                        projection.Delta.X,
                        projection.Delta.Y,
                        GetMapDimension(), GetMapContainerDimension(),
                        false
                        );
                }
                //SetMapCanvasBackgroundByMoveStatus();



                //SetMapCanvasBackgroundByMoveStatus();
            }
        }

        private void HandleOffscreenRelease()
        {
            log.Info("Offscreen tap release");

            if (ProjectionRelease != null)
                ProjectionRelease();

            MapMoveStatus = MapMoveStatuses.NotMoving;
            //MapCanvas.Background = new SolidColorBrush(Colors.White);

        }

        private void SetupTargetTimer()
        {
            TargetPositionChanged += MapControl_MapContainerPositionChanged;

            targetTimer.Tick += (s, e) =>
            {
                // user has held target in bulls eye for the time span required
                log.Verbose("target hold span achieved");

                targetTimer.Stop();

                if (TargetHoldAchieved != null)
                    TargetHoldAchieved();
            };
        }

        private void MapControl_MapContainerPositionChanged(Point newPosition)
        {
            //log.Verbose("Map moved in container: " + newPosition + "  ||| " + IsInTarget(newPosition));

            HandleNewTargetStatus(IsInTarget(newPosition));
        }

        private void HandleNewTargetStatus(bool isInTarget)
        {
            bool targetStatusChanged = (isInTarget != IsTargetInBullsEye);

            if (targetStatusChanged)
            {
                BUllsEyeEllipse.Stroke = new SolidColorBrush(isInTarget ?
                    BULLEYE_TARGET_COLOR :
                    BULLEYE_NONTARGET_COLOR
                    );

                log.Verbose("Target {0} the bulls eye", isInTarget ? "entered" : "left");

                if (isInTarget)
                {
                    targetTimer.Start();
                    //TargetEntered();
                    BullsEyeEntryCount++;
                }
                else
                    targetTimer.Stop();
            }

            IsTargetInBullsEye = isInTarget;
        }

        public static Color GetTickBackgroundColor(double tickValue)
        {
            return (tickValue < 0) ? TICK_NEGATIVE_COLOR : TICK_POSITIVE_COLOR;
        }

        public Point BullsEyeCenterPosition
        {
            get
            {
                var e = BUllsEyeEllipse;
                return new Point(
                    Canvas.GetLeft(e) + e.Width / 2.0 + e.StrokeThickness / 2.0,
                    Canvas.GetTop(e) + e.Height / 2.0 + e.StrokeThickness / 2.0
                    );
            }
            set
            {
                SetEllipseCenterPositionInCanvas(BUllsEyeEllipse, value);
            }
        }



        #endregion
        #region Methods 

        public void Reset()
        {
            BullsEyeCenterPosition = ViewPortCenter;
            BUllsEyeEllipse.Stroke = new SolidColorBrush(BULLEYE_NONTARGET_COLOR);

            InertialDistanceTraveled = 0;
            NoninertialDistanceTraveled = 0;
            OvershootTravelDistance = 0;
            TouchCount = 0;
            BullsEyeEntryCount = 0;

            MapMoveStatus = MapMoveStatuses.NotMoving;

            LastOffscreenProjection = null;
        }

        private static void SetEllipseCenterPositionInCanvas(Ellipse e, Point center)
        {
            SetEllipseCenterPositionInCanvas(e, center.X, center.Y);
        }

        private static void SetEllipseCenterPositionInCanvas(Ellipse e, double centerX, double centerY)
        {
            SetPositionInCanvas(e,
                centerX - e.Width / 2.0 - e.StrokeThickness / 2.0,
                centerY - e.Height / 2.0 - e.StrokeThickness / 2.0
                );
        }

        private void InitializeBullsEye()
        {
            //double origoX = mapSize.Width / 2;
            //var mapContainerOrigo = new Point(Width / 2.0, Height / 2.0);

            //var size = new Size(BullsEyeSize, BullsEyeSize);
            //var bullsEyeRect = new Rect(
            //    new Point(Center.X - size.Width / 2.0, Center.Y - size.Height / 2.0),
            //    size
            //    );

            //BullsEyeSize.C
            BUllsEyeEllipse.Stroke = new SolidColorBrush(BULLEYE_NONTARGET_COLOR);


            SetEllipseCenterPositionInCanvas(BUllsEyeEllipse, ViewPortCenter);

            //setPositionInCanvas(BUllsEyeEllipse, bullsEyeRect.X, bullsEyeRect.Y);
            //Canvas.SetLeft(BUllsEyeEllipse, );
            //Canvas.SetTop(BUllsEyeEllipse, mapContainerSize.Height / 2 - BUllsEyeEllipse.ActualHeight / 2);
            //Canvas.SetLeft(BUllsEyeEllipse, origoX - BUllsEyeEllipse.ActualHeight /2);
        }

        //private double ActualWidth(DependencyObject element)
        //{
        //    return (double)element.GetValue(ActualWidthProperty);
        //}

        //private double ActualHeight(DependencyObject element)
        //{
        //    return (double)element.GetValue(ActualHeightProperty);
        //}

        private void SetMapPositionCenter()
        {
            //MapCanvas.Width = mapSize; //(tickCount + 1) * tickDistance + 2 * sideBufferWidth;
            //MapCanvas.Height = mapSize; // parentSize.Height;

            //double origo = new Point(Width Height / 2.0);
            // set map location to neutral

            SetPositionInCanvas(
                MapCanvas,
                -MapCanvasCenter.X + ViewPortCenter.X,
                -MapCanvasCenter.Y + ViewPortCenter.Y
                );

            //log.Verbose(CanvasContainer.ActualHeight + "********************");
        }

        public async void OptimizeByConvertMapChildrenToSingleImage()
        {
            BUllsEyeEllipse.Visibility = Visibility.Collapsed;

            MapCanvas.Background = new SolidColorBrush(Colors.Transparent);

            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(MapCanvas);//, (int)MapCanvas.Width, (int)MapCanvas.Height);
            var img = new Image { Source = renderTargetBitmap };

            //img.Stretch = Stretch.Fill;

            img.Height = MapCanvas.Height;
            img.Width = MapCanvas.Width;

            MapChildren = new List<FrameworkElement> { img };

            BUllsEyeEllipse.Visibility = Visibility.Visible;

            SetMapCanvasBackgroundByMoveStatus();

        }

        public IEnumerable<FrameworkElement> MapChildren
        {
            set
            {
                MapCanvas.Children.Clear();
                foreach (var el in value)
                    MapCanvas.Children.Add(el);

                //MapMoveStatus = MapMoveStatuses.NotMoving;

                //if (targetPoint == null)
                //    throw new Exception("Target point was not set.");
            }
        }

        //private static void setPositionInCanvas(FrameworkElement el, Point p)
        //{
        //    Canvas.SetLeft(el, p.X);
        //    Canvas.SetTop(el, p.Y);
        //}

        private static void SetPositionInCanvas(FrameworkElement el, double x, double y)
        {
            Canvas.SetLeft(el, x);
            Canvas.SetTop(el, y);
        }

        private static void AddToPositionInCanvas(FrameworkElement el, double x, double y)
        {
            Canvas.SetLeft(el, Canvas.GetLeft(el) + x);
            Canvas.SetTop(el, Canvas.GetTop(el) + y);
        }

        #endregion
        #region Event handlers

        private Rect GetUIRect(FrameworkElement el)
        {
            return new Rect(
                new Point(Canvas.GetLeft(el), Canvas.GetTop(el)),
                new Size(el.ActualWidth, el.ActualHeight)
                );
        }

        private static bool IsWithinBounds(double value, double min, double max)
        {
            return value >= min && value <= max;
        }

        private void InputCanvas_ManipulationDelta(object sender, Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
        {
            bool preventInertia = IsOffscreenSpaceEnabled;
            var delta = e.Delta.Translation;
            var velocity = e.Velocities.Linear;

            //var velocityLength = Math.Sqrt(Math.Pow(velocity.X,2) + Math.Pow(velocity.Y,2)); 
            //if (velocityLength > AppSettings.MAX_SWIPE_VELOCITY) {
            //    double leveler = AppSettings.MAX_SWIPE_VELOCITY / velocityLength;
            //    velocity.X  = velocity.X * leveler;
            //    velocity.Y = velocity.Y * leveler;

            //    log.Verbose("velocity vector was scaled by: " + leveler);
            //}

            var velocityLength = Math.Sqrt(Math.Pow(velocity.X, 2) + Math.Pow(velocity.Y, 2));
            if (velocityLength > AppSettings.MAX_SWIPE_VELOCITY)
            {
                double leveler = AppSettings.MAX_SWIPE_VELOCITY / velocityLength;
                velocity.X = velocity.X * leveler;
                velocity.Y = velocity.Y * leveler;

                //log.Verbose("velocity vector was scaled by: " + leveler);
            }

            if (e.IsInertial)
            {
                if (preventInertia)
                    return;

                //log.Verbose(e.Velocities.Linear.ToString());

                delta.X = delta.X * Math.Pow(AppSettings.INERTIA_FACTOR, -LastNonIntertialInputVelocity.X / velocity.X);
                delta.Y = delta.Y * Math.Pow(AppSettings.INERTIA_FACTOR, -LastNonIntertialInputVelocity.Y / velocity.Y);


            }
            else {
                LastNonIntertialInputVelocity = e.Velocities.Linear; //velocity;
            }

            Rect mapRect = GetMapDimension();
            Rect mapContainerRect = GetMapContainerDimension();

            var fingerPositionInMapContainer = new Point(
                mapContainerRect.Left + mapRect.Left + e.Position.X,
                mapContainerRect.Top + mapRect.Top + e.Position.Y
                );



            //log.Verbose("(x:" + positionInMapContainer.X + "  y:" + positionInMapContainer.Y + ")");

            bool isTouchOutsideCanvas = !mapContainerRect.Contains(fingerPositionInMapContainer);
            if (isTouchOutsideCanvas && !e.IsInertial)
            {
                if (MapMoveStatus != MapMoveStatuses.MoveByOffScreen)
                {
                    //log.Verbose("Onscreen delta is outside map container bounds");

                    //log.Verbose("{0} | {1}  | {2}", fingerPositionInMapContainer, mapContainerRect.Width, mapContainerRect.Height);

                    MapMoveStatus = IsOffscreenSpaceEnabled ?
                        MapMoveStatuses.MoveByOffScreen :
                        MapMoveStatuses.NotMoving;
                }

                return;
            }

            //Point delta = e.Delta.Translation;
            //double deltaX = e.Delta.Translation.X;
            //double deltaY = e.Delta.Translation.Y;

            //bool isDeltaInCanvas = deltaX <= canvas.Width && deltaY <= canvas.Height;
            //if (!isDeltaInCanvas)
            //    return;

            PerformMove(delta.X, delta.Y, mapRect, mapContainerRect, e.IsInertial);
        }

        private void PerformMove(double deltaX, double deltaY, Rect mapRect, Rect mapContainerRect, bool isInertial)
        {
            if (double.IsNaN(deltaX))
                deltaX = 0;
            if (double.IsNaN(deltaY))
                deltaY = 0;

            if (MoveBullsEyeInsteadOfMap) // = user is in value-estimation mode
            {

                AddToPositionInCanvas(BUllsEyeEllipse, deltaX, deltaY);

                UIChange();

                TargetPositionChanged(new Point(
                    mapContainerRect.Left + mapRect.Left + Target.X,
                    mapContainerRect.Top + mapRect.Top + Target.Y
                    ));

                return;
            }

            //if (!IsMapMoveEnabled)
            //{
            //    deltaX = -deltaX;
            //    deltaY = -deltaY;
            //}

            var newX = mapRect.Left + deltaX;
            var newY = mapRect.Top + deltaY;

            //var parentSize = ActualSize(canvas.Parent);


            var minX = -mapRect.Width + mapContainerRect.Width;
            var maxX = 0;//parentSize.Width - canvas.ActualWidth;

            var minY = -mapRect.Height + mapContainerRect.Height;
            var maxY = 0;

            bool willDeltaXKeepMapInItsContainer = IsWithinBounds(newX, minX, maxX); //(newX >= minX && newX <= maxX);
            bool willDeltaYKeepMapInItsContainer = IsWithinBounds(newY, minY, maxY); //(newY >= minY && newY <= maxY);


            //bool isHorisontalMoveEnabled = (trial.mode == TrialMode.Horisontal || trial.mode == TrialMode.TwoDimensional);
            //bool isVerticalMoveEnabled = (trial.mode == TrialMode.Vertical || trial.mode == TrialMode.TwoDimensional);

            //IsHorsontalMapMoveAllowed = true;
            //IsVerticalMapMoveAllowed = true;

            //log.Verbose(willDeltaXKeepMapInItsContainer + " : " + willDeltaYKeepMapInItsContainer);

            //bool isPointerOutsideMapContainerBounds = (!willDeltaXKeepMapInItsContainer || !willDeltaYKeepMapInItsContainer);
            //if (isPointerOutsideMapContainerBounds) {
            //    //TODO

            //    MapMoveStatus = MapMoveStatuses.MoveByOffScreen;

            //    return;
            //}

            //log.Verbose("{0} | {1}  | {2}", newX, minX, maxX);

            bool isHorisontalMove = IsHorsontalMapMoveAllowed && willDeltaXKeepMapInItsContainer;
            if (isHorisontalMove)
                Canvas.SetLeft(MapCanvas, newX);

            bool isVerticalMove = IsVerticalMapMoveAllowed && willDeltaYKeepMapInItsContainer;
            if (isVerticalMove)
                Canvas.SetTop(MapCanvas, newY);

            bool isMapMoved = isHorisontalMove || isVerticalMove;

            if (isMapMoved & TargetPositionChanged != null)
            {
                TargetPositionChanged(new Point(
                    mapContainerRect.Left + mapRect.Left + Target.X,
                    mapContainerRect.Top + mapRect.Top + Target.Y
                    ));

                double travelDelta = Math.Abs(isVerticalMove ? deltaY : 0) + Math.Abs(isHorisontalMove ? deltaX : 0);
                if (isInertial)
                    InertialDistanceTraveled += travelDelta;
                else
                    NoninertialDistanceTraveled += travelDelta;

                if (BullsEyeEntryCount >= 1 && !IsTargetInBullsEye)
                    OvershootTravelDistance += travelDelta;

                UIChange();
            }

            //lastMapContainerTouchPoint = fingerPositionInMapContainer;
        }

        private Rect GetMapDimension()
        {
            //if (e.IsInertial)
            //    return;

            //Canvas canvas = (Canvas)sender;

            //Rect canvasRect = new Rect(
            //    new Point(Canvas.GetLeft(canvas), Canvas.GetTop(canvas)),
            //    new Size(canvas.Width, canvas.Height)
            //    );

            return GetUIRect(MapCanvas);
        }

        private Rect GetMapContainerDimension()
        {
            return GetUIRect(MapContainerCanvas);
        }

        private bool IsInTarget(Point positionInViewport)
        {
            double maxRadius = BullsEyeSize / 2.0;

            Point releasePositionInViewPort = BullsEyeCenterPosition;

            FromTargetToReleasePosition = new Point(
                releasePositionInViewPort.X - positionInViewport.X,
                releasePositionInViewPort.Y - positionInViewport.Y
                //ViewPortCenter.X - positionInViewport.X,
                //ViewPortCenter.Y - positionInViewport.Y
                );

            //SetEllipseCenterPositionInCanvas


            double radius = Math.Sqrt(
                //Math.Pow(ViewPortCenter.X - positionInViewport.X, 2) + Math.Pow(ViewPortCenter.Y - positionInViewport.Y, 2)
                Math.Pow(releasePositionInViewPort.X - positionInViewport.X, 2) + Math.Pow(releasePositionInViewPort.Y - positionInViewport.Y, 2)
                );

            //log.Verbose("R:" +radius);
            return radius <= maxRadius;
        }

        #endregion
        #region Properties

        public Point Target { get; set; }

        public Point ViewPortCenter
        {
            get { return new Point(Width / 2.0, Height / 2.0); }
        }

        public Point MapCanvasCenter
        {
            get { return new Point(MapCanvas.Width / 2.0, MapCanvas.Height / 2.0); }
        }

        public double BullsEyeSize { get; set; }
        //{
        //    get { return 2 * TICK_DISTANCE; }
        //}

        //public bool AutoConnectOnStartup
        //{
        //    get { return AppSettings.AutoConnectOnStartup; }
        //    set { AppSettings.AutoConnectOnStartup = value; }
        //}

        //public string MarkerName
        //{
        //    get { return AppSettings.MarkerName; }
        //    set { AppSettings.MarkerName = value; }
        //}

        //public Rect BullsEyeRect
        //{
        //    get {
        //        if (bullsEyeRect2 == null)
        //            bullsEyeRect2 = new Rect(
        //                new Point()
        //                );

        //        return bullsEyeRect2.Value;
        //    }
        //}

        //public double BUllsEyeEllipseLeft
        //{
        //    get {
        //        log.Info("ccccccccccccccccccccccc");
        //        return ActualWidth(BUllsEyeEllipse.Parent);
        //    }
        //}

        //public double BUllsEyeEllipseTop
        //{
        //    get { return 1; }
        //}

        public Size MapSize
        {
            set
            {
                MapCanvas.Width = value.Width; //(tickCount + 1) * tickDistance + 2 * sideBufferWidth;
                MapCanvas.Height = value.Height; // parentSize.Height;

                SetMapPositionCenter();
            }
        }

        public bool MoveBullsEyeInsteadOfMap { get; set; }

        public bool IsHorsontalMapMoveAllowed { get; set; }

        public bool IsVerticalMapMoveAllowed { get; set; }

        public bool IsTargetInBullsEye { get; private set; }

        public bool IsOffscreenSpaceEnabled { get; set; }

        public bool IsModeModeColoringEnabled { get; set; }

        public MapMoveStatuses MapMoveStatus
        {
            get { return mapMoveStatus; }
            set
            {
                if (mapMoveStatus != value)
                {
                    mapMoveStatus = value;

                    log.Info("Map move status is now: " + mapMoveStatus);

                    SetMapCanvasBackgroundByMoveStatus();
                }
            }
        }

        public ProjectionMode MapMoveProjectionMode { get; internal set; }
        public Point LastNonIntertialInputVelocity { get; private set; }
        public Point FromTargetToReleasePosition { get; set; }

        public double InertialDistanceTraveled { get; set; }
        public double NoninertialDistanceTraveled { get; set; }

        public int TouchCount { get; set; }


        public bool IsMapMoveEnabled { get; set; }

        public ProjectedXYPoint LastOffscreenProjection { get; private set; }

        public double OvershootTravelDistance { get; private set; }

        private void SetMapCanvasBackgroundByMoveStatus()
        {
            //log.Verbose(MapMoveStatus + ":" + MapCanvasMoveBackgrounds[MapMoveStatus]);
            //log.Verbose(MapMoveStatus.ToString());

            //if (MapMoveStatus == MapMoveStatuses.MoveByOnScreen)
            //    MapCanvas.Background = new SolidColorBrush(Colors.Blue);

            //if (MapMoveStatus == MapMoveStatuses.NotMoving)
            //    MapCanvas.Background = new SolidColorBrush(Colors.Pink);

            MapCanvas.Background = new SolidColorBrush(
                IsModeModeColoringEnabled ?
                MapCanvasMoveBackgrounds[MapMoveStatus] :
                MapCanvasMoveBackgrounds[MapMoveStatuses.NotMoving]
                );
        }

        #endregion

        private void MapContainerCanvas_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            //e.po

        }

        private void MapContainerCanvas_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            //SetMapCanvasBackgroundByMoveStatus();
            //MapCanvas.Background = new SolidColorBrush(Colors.LightBlue);

            MapMoveStatus = MapMoveStatuses.MoveByOnScreen;
            TouchCount++;

            if (MoveBullsEyeInsteadOfMap)
            {
                var p = e.GetCurrentPoint(MapContainerCanvas).Position;
                SetEllipseCenterPositionInCanvas(BUllsEyeEllipse, p.X, p.Y);
                //SetPositionInCanvas();
            }

        }



        private void MapContainerCanvas_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            log.Verbose("Onscreen pointer exited map");


            //MapMoveStatus = MapMoveStatuses.NotMoving;

            MapMoveStatus = IsOffscreenSpaceEnabled ?
                MapMoveStatuses.MoveByOffScreen :
                MapMoveStatuses.NotMoving;

            SetMapCanvasBackgroundByMoveStatus();

            if (ScreenRelease != null)
                ScreenRelease();
            //if(!IsOffscreenSpaceEnabled)
            //    Rele
            ////Rect mapContainerDimension = GetMapContainerDimension();

            //var edgeResiduals = new double[]
            //{
            //    lastMapContainerTouchPoint.X,
            //    mapContainerDimension.Width - lastMapContainerTouchPoint.X,
            //    lastMapContainerTouchPoint.Y,
            //    mapContainerDimension.Height - lastMapContainerTouchPoint.Y
            //};

            //bool isLastTouchInOffscreenTransitionBuffer = edgeResiduals.Where(r => r <= MAP_OFFSCREEN_TRANSITION_PIXEL_COUNT_BUFFER).Count() > 0;

            //if (isLastTouchInOffscreenTransitionBuffer)
            //{
            //    MapMoveStatus = MapMoveStatuses.MoveByOffScreen;
            //}

        }

        public Action<PlanePoint> ProjectionPreProcess { get; set; }

    }
}
