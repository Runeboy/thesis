using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Input.Inking;
using Windows.Devices.Input;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml.Shapes;
using Airswipe.WinRT.Core.Log;
using Windows.UI.Xaml.Controls;
using Airswipe.WinRT.Core;
using Airswipe.WinRT.UI.Common;
using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.Core.Data;
using System.Linq;
using System.Diagnostics;
using Airswipe.WinRT.Core.Data.Dto;

// Extension of MS example code from https://msdn.microsoft.com/en-us/library/windows/apps/xaml/dn792131.aspx
namespace Airswipe.WinRT.UI.Pages
{
    public sealed partial class CalibratePage : BasicPage
    {
        #region Fields

        private static ILogger log = new TypeLogger<CalibratePage>();

        const double STROKE_THICKNESS = 5;
        static readonly Color LINE_COLOR = Colors.Red;
        const double MIN_DRAW_DISTANCE = 2;
        static readonly Color RECT_COLOR = Colors.Blue;
        private readonly double MINIMUM_DIAGONAL_LENGTH = 300;

        InkManager inkManager = new InkManager();

        PlanePoint previousDrawtPoint;
        uint touchInputID = 0;

        LinkedList<CapturedInputPoint> touchPoints = new LinkedList<CapturedInputPoint>();

        DateTime callibrationStartTime;

        private static readonly RectLinePoints<SpatialPoint> enumalatedOffScreenPoints = new RectLinePoints<SpatialPoint>
            {
                Top = new List<SpatialPoint> {
                    new XYZPoint(1.1, 1.9, 1.1),
                    new XYZPoint(2.9, 2.1, 0.9),
                    },
                Right = new List<SpatialPoint> {
                    new XYZPoint(2.9, 2.1, 0.9),
                    new XYZPoint(3.1, 0.9, 1.1),
                    },
                Bottom = new List<SpatialPoint> {
                    new XYZPoint(3.1, 0.9, 1.1),
                    new XYZPoint(0.9, 1.1, 0.9),
                    },
                Left = new List<SpatialPoint> {
                    new XYZPoint(0.9, 1.1, 0.9),
                    new XYZPoint(1.1, 1.9, 1.1),
                    }
        };
        //private int onscreenSkipCount;
        private DateTime callibrationEndTime;
        private OnscreenPoint lastMapCallibrationTouchPoint;

        bool isCallibrating;

        #endregion
        #region Constructor

        public CalibratePage()
        {
            this.InitializeComponent();

            InkCanvas.PointerPressed += new PointerEventHandler(InkCanvas_PointerPressed);
            InkCanvas.PointerMoved += new PointerEventHandler(InkCanvas_PointerMoved);
            InkCanvas.PointerReleased += new PointerEventHandler(InkCanvas_PointerReleased);
            InkCanvas.PointerExited += new PointerEventHandler(InkCanvas_PointerReleased);

            AppMotionTrackerClient.Instance.TrackedPointReady += Instance_TrackedPointReady;

            //AppMotionTrackerClient.Instance.OnFrameBatchReady 

            Loaded += CalibratePage_Loaded;
            Unloaded += CalibratePage_Unloaded;

            ShowInfoPopupOnLeftEdgeTap();


        }

        #endregion
        #region Event handlers

        private void ShowInfoPopupOnLeftEdgeTap()
        {
            EdgeTap += (RectLineBoundary location) =>
            {
                if (!InfoPopup.IsOpen && location == RectLineBoundary.Left)
                    InfoPopup.IsOpen = true;
            };
        }

        private void CalibratePage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            DrawInputIfSaved();
        }

        private void CalibratePage_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
        }

        private void Instance_TrackedPointReady(OffscreenPoint[] t)
        {
            var trackPoint = t[0];
            TrackedPointerLocationTextBlock.Text = String.Format("Tracked Pointer: (\n\t{0}, \n\t{1}, \n\t{2})", trackPoint.X, trackPoint.Y, trackPoint.Z);

            //bool isLastOnscreenTouchFreenToPair = (
            //    touchPoints.Count() > 0 && touchPoints.Last().Onscreen.CaptureTime == lastMapCallibrationTouchPoint.CaptureTime
            //    );


            //OnscreenPoint p;
            //lock (lastMapCallibrationTouchPoint)
            //{
            //    p = lastMapCallibrationTouchPoint;
            //    lastMapCallibrationTouchPoint = null;
            //}

            if (isCallibrating && lastMapCallibrationTouchPoint != null)
            {
                touchPoints.AddLast(new CapturedInputPoint{
                    Onscreen = lastMapCallibrationTouchPoint,
                    Offscreen = trackPoint
                });
                lastMapCallibrationTouchPoint = null;
                TouchPointCountTextBlock.Text = "Point pairing count: " + touchPoints.Count();
            }
        }

        // Initiate ink capture.
        public void InkCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //onscreenSkipCount = 0;
            callibrationStartTime = DateTime.Now;

            // Get information about the pointer location.
            PointerPoint pt = e.GetCurrentPoint(InkCanvas);
            previousDrawtPoint = XYPoint.FromPoint(pt.Position);

            // Accept input only from a pen or mouse with the left button pressed. 
            //PointerDeviceType pointerDevType = e.Pointer.PointerDeviceType;
            //bool isInputAllowed = (pointerDevType == PointerDeviceType.Touch || pointerDevType == PointerDeviceType.Pen);
            //if (!isInputAllowed)
            //    return;
            ////||
            //    pointerDevType == PointerDeviceType.Mouse &&
            //    pt.Properties.IsLeftButtonPressed

            InkCanvas.Children.Clear();

            touchPoints.Clear();

            //lock(lastMapCallibrationTouchPoint) { 
                lastMapCallibrationTouchPoint = OnscreenPoint.FromPoint(pt.Position);
            //}

            isCallibrating = true;
            //AddTouchPoint(pt.Position);

            // Pass the pointer information to the InkManager.
            inkManager.ProcessPointerDown(pt);
            touchInputID = pt.PointerId;

            e.Handled = true;
        }

        //private void AddTouchPoint(Point pt)
        //{
        //    touchPoints.AddLast(new CapturedInputPoint
        //    {
        //        Onscreen = OnscreenPoint.FromPoint(pt),
        //        Offscreen = AppMotionTrackerClient.Instance.LastPoint
        //    });
        //}


        // Draw on the canvas and capture ink data as the pointer moves.
        public void InkCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId == touchInputID)
            {
                PointerPoint pt = e.GetCurrentPoint(InkCanvas);

                // Render a red line on the canvas as the pointer moves. 
                // Distance() is an application-defined function that tests
                // whether the pointer has moved far enough to justify 
                // drawing a new line.


                //var lastPoint = AppMotionTrackerClient.Instance.LastPoint;

                //bool isNewOffscreenPointRecievedSinceLast = 
                //    (lastPoint.CaptureTime >  
                //    touchPoints.Last().Offscreen.CaptureTime
                //    );

                //if (!isNewOffscreenPointRecievedSinceLast)
                //    onscreenSkipCount++;
                //else
                //    AddTouchPoint(pt.Position);

                //if (Distance(currentContactPt, previousContactPt) > MIN_DRAW_DISTANCE)

                lastMapCallibrationTouchPoint = OnscreenPoint.FromPoint(pt.Position);


                PlanePoint currentContactPt = XYPoint.FromPoint(pt.Position);
                if (currentContactPt.DistanceFrom(previousDrawtPoint) > MIN_DRAW_DISTANCE)
                {
                    drawLine(previousDrawtPoint, currentContactPt);

                    previousDrawtPoint = currentContactPt;

                    // Pass the pointer information to the InkManager.
                    inkManager.ProcessPointerUpdate(pt);
                }
            }

            e.Handled = true;
        }

        private TimeSpan CallibrationDuration { get { return callibrationEndTime - callibrationStartTime; } }

        // Finish capturing ink data and use it to render ink strokes on 
        // the canvas. 
        public async void InkCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            lock (this) // we may have multiple release events
            {
                if (e.Pointer.PointerId != touchInputID)
                    return;
                touchInputID = 0;
            }

            isCallibrating = false;
            callibrationEndTime = DateTime.Now;

            log.Info("Callibration time span: " + CallibrationDuration);


            PointerPoint pt = e.GetCurrentPoint(InkCanvas);

            //touchPoints.Add(pt.Position);

            // Pass the pointer information to the InkManager. 
            inkManager.ProcessPointerUp(pt);


            // Call an application-defined function to render the ink strokes.
            //RenderAllStrokes();
            e.Handled = true;

            //log.Info("Onscreen points skipped due to offscreen lag: {0}/{1}", onscreenSkipCount, onscreenSkipCount+ touchPoints.Count());

            try
            {
                DeriveAndSaveInputDimension();
                DrawInputIfSaved();

                MasterPage.RequestCursorVisibility(true);
            }
            catch (InputDimensionException ex)
            {
                InkCanvas.Children.Clear();

                //ShowMessageDialog(ex.Message);
                //MessageExpert.ShowMessageDialog(ex.Message);

                log.Error(ex.Message);

                await new Windows.UI.Popups.MessageDialog(ex.Message).ShowAsync();
            }
        }

        //private async void ShowMessageDialog(string message)
        //{
        //    //await new Windows.UI.Popups.MessageDialog(message).ShowAsync();
        //    MessageExpert.ShowMessageDialog(message);
        //}


        private void HidePopupButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            InfoPopup.IsOpen = false;
        }

        private void ScalePlaneUnitButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var space = AppSettings.InputSpace;
            var plane = space.Offscreen;

            var scaleRatio = 1 / plane.NormalVector.Length;

            plane.NormalVector = plane.NormalVector.Normalize();
            plane.Offset = scaleRatio * plane.Offset;

            AppSettings.InputSpace = space;
        }

        //private void AdjustPlaneNormalButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        //{
        //    var space = AppSettings.InputSpace;

        //    space.Offscreen.NormalVector = AppMotionTrackerClient.Instance.LastPoint.Direction.Multiply(-1).Normalize();

        //    AppSettings.InputSpace = space;
        //}

        #endregion
            #region Methods

        private void drawLine(PlanePoint previousContactPt, PlanePoint currentContactPt)
        {
            Line line = new Line()
            {
                X1 = previousContactPt.X,
                Y1 = previousContactPt.Y,
                X2 = currentContactPt.X,
                Y2 = currentContactPt.Y,
                StrokeThickness = STROKE_THICKNESS,
                Stroke = new SolidColorBrush(LINE_COLOR)
            };


            // Draw the line on the canvas by adding the Line object as
            // a child of the Canvas object.
            InkCanvas.Children.Add(line);
        }

        private void DrawInputIfSaved()
        {
            InputSpace inputs = AppSettings.InputSpace;
            if (inputs == null)
                return;

            var onscreenDimensions = inputs.Onscreen;
            var r = new Rectangle { Width = onscreenDimensions.Width, Height = onscreenDimensions.Height, Fill = new SolidColorBrush(RECT_COLOR), Opacity = 0.5 };

            DimsTextBlock.Text = string.Format(
                "Size: {0}, {1}",
                Math.Round(onscreenDimensions.Width * 10) / 10.0,
                Math.Round(onscreenDimensions.Height * 10) / 10.0
                );

            Canvas.SetLeft(r, onscreenDimensions.X);
            Canvas.SetTop(r, onscreenDimensions.Y);
            //Canvas.SetZIndex(r, -1);

            InkCanvas.Children.Add(r);

            log.Info("Input rectangle drawn from storage");
        }

        private void DeriveAndSaveInputDimension()
        {

            List<OnscreenPoint> onscreenPoints = touchPoints.Select(p => p.Onscreen).ToList();
            List<OffscreenPoint> offscreenPoints = touchPoints.Select(p => p.Offscreen).ToList();

            Rect maximizedOnscreenRect = GetMaximizedRectSpannedByPlanePoints(onscreenPoints.Cast<PlanePoint>());

            if (GetRectangleDiagonalLength(maximizedOnscreenRect) < MINIMUM_DIAGONAL_LENGTH) 
                throw new InputDimensionException("Input dimension diagonal was below the minimum.");

            ErrorIfLessThanExpectedTouchPointsCollected();

            // todo: do for every corner
            var idealCornerOnscreenPoints = new Dictionary<RectJointBoundary, PlanePoint> {
                { RectJointBoundary.TopLeft, new XYPoint(maximizedOnscreenRect.X, maximizedOnscreenRect.Y) },
                { RectJointBoundary.TopRight, new XYPoint(maximizedOnscreenRect.X + maximizedOnscreenRect.Width, maximizedOnscreenRect.Y) },
                { RectJointBoundary.BottomLeft, new XYPoint(maximizedOnscreenRect.X, maximizedOnscreenRect.Y + maximizedOnscreenRect.Height) },
                { RectJointBoundary.BottomRight, new XYPoint(maximizedOnscreenRect.X + maximizedOnscreenRect.Width, maximizedOnscreenRect.Y + maximizedOnscreenRect.Height) },
            };

            //var smoothedOffscreenCornerPoints = new Dictionary<PlaneLocation, SpacialPoint>();
            //var smoothedOffscreenCornerPoints = new List<SpatialPoint>();

            Dictionary<RectJointBoundary, CapturedInputPoint> onscreenCornersClosestToIdeal = GetCapturedPointsClosestToOnscreenIdeal(idealCornerOnscreenPoints);
            Rect onscreenPlane = GetMaximizedRectSpannedByPlanePoints(
                onscreenCornersClosestToIdeal.Values.Select(cp => (PlanePoint)cp.Onscreen)
                );
            //double onscreenRatio = onscreenPlane.Width / onscreenPlane.Height;

            Dictionary<RectJointBoundary, IEnumerable<SpatialPoint>> offscreenRectLinePoints = GetOffscreenCornerPointSets(onscreenCornersClosestToIdeal);

            XYZRect offscreenRectPlane = GetSpatialRectFromOffscreenCornerPointSets(offscreenRectLinePoints);
            //offscreenRectPlane.

            var onscreenSideRatio = (onscreenPlane.Width / onscreenPlane.Height);
            offscreenRectPlane.SideRatio = onscreenSideRatio;

            AppSettings.InputSpace = new InputSpace(onscreenPlane, offscreenRectPlane);

            log.Info("Input space stored");
        }

        private void ErrorIfLessThanExpectedTouchPointsCollected()
        {
            double minExpectedFrameCountBasedTrackerFPS = AppSettings.MIN_EXPECTED_FPS * CallibrationDuration.TotalSeconds;
            if (minExpectedFrameCountBasedTrackerFPS > touchPoints.Count())
                throw new Exception(string.Format("Expected at least {0} touch points, but only {1} were collected.", minExpectedFrameCountBasedTrackerFPS, touchPoints.Count()));
        }

        private Dictionary<RectJointBoundary, IEnumerable<SpatialPoint>> GetOffscreenCornerPointSets(Dictionary<RectJointBoundary, CapturedInputPoint> onscreenCornersClosestToIdeal)
        {
            var result = new Dictionary<RectJointBoundary, IEnumerable<SpatialPoint>>();

            int smoothingPointNeighbourExpandCount = (int)OffscreenSmoothCountDoubleBox.Value;
            foreach (RectJointBoundary corner in FourCorners)
            {
                log.Verbose("Offscreen, finding (smoothed) corner..");

                CapturedInputPoint idealCapturedCornerTouchPoint = onscreenCornersClosestToIdeal[corner];

                var cornerTime = idealCapturedCornerTouchPoint.Onscreen.CaptureTime;

                // TODO: use other measure than time if points are not accumulated based on tracker feed
                //var offscreenPointsIncreasingTimeFromCorner = touchPoints.Select(
                //    t => new
                //    {
                //        Offscreen = t.Offscreen,
                //        MillisecondsFromCorner = Math.Abs(cornerTime.Subtract(t.Onscreen.CaptureTime).TotalMilliseconds)
                //    }
                //    ).OrderBy(o => o.MillisecondsFromCorner).ToList();

                //string skod = offscreenPointsIncreasingTimeFromCorner.Select(p => p.MillisecondsFromCorner.ToString()).Aggregate((p1, p2) => p1 + ", " + p2);

                //var offscreenSmoothingPoints = offscreenPointsIncreasingTimeFromCorner.Take(smoothingPointNeighbourExpandCount).ToList();

                var listRef = touchPoints.Find(idealCapturedCornerTouchPoint);

                List<CapturedInputPoint> offscreenSmoothingPoints;

                int setSize = (int)OffscreenSmoothCountDoubleBox.Value;
                if (setSize % 2 != 1)
                    throw new InputDimensionException("Offscreen smoothing count must be unequal");

                try
                {

                    var back = listRef;
                    var forward = listRef;

                    offscreenSmoothingPoints = new List<CapturedInputPoint> { listRef.Value };
                    for(int i = 1; i <= setSize/2; i++)
                    {
                        back = back.Previous;
                        forward = back.Next;

                        offscreenSmoothingPoints.Add(back.Value);
                        offscreenSmoothingPoints.Add(forward.Value);
                    }
                    //{
                    //        listRef.Previous.Value,
                    //        listRef.Value, // I.E. HARDCODED TO 3 now
                    //        listRef.Next.Value,
                    //};
                }
                catch(Exception e)
                {
                    throw new InputDimensionException("Error during derivation of offscreen corner points sets (all corners may not be available)", e);
                }

                //log.Verbose("Collected {0} smoothing points, time from onscreen corner capture time: {1} ms (first) - ms {2} (last) ", offscreenSmoothingPoints.Count(), offscreenSmoothingPoints.First().MillisecondsFromCorner, offscreenSmoothingPoints.Last().MillisecondsFromCorner);

                if (IsSmoothingTimeWindowEnforcedCheckBox.IsChecked == true)
                {
                    var smoothingMillisecondsWindow = SmoothingMaxDiversionMilliSecondsValueBox.Value;

                    log.Verbose("Enforcing that offscreen point smoothing occurs within max time  from onscreen corner point: {0} ms ..", smoothingMillisecondsWindow);

                    var timeSorted = offscreenSmoothingPoints.Select(
                        t => new
                        {
                            Offscreen = t.Offscreen,
                            MillisecondsFromCorner = Math.Abs(cornerTime.Subtract(t.Onscreen.CaptureTime).TotalMilliseconds)
                        }
                        ).OrderBy(o => o.MillisecondsFromCorner).ToList();

                    var offscreenPointMaxCapturedTimeOutsideAllowedTimeSpanCount = timeSorted.Count(
                        o => (o.MillisecondsFromCorner > smoothingMillisecondsWindow)
                        );

                    string skod = timeSorted.Select(p => p.MillisecondsFromCorner.ToString()).Aggregate((p1, p2) => p1 + ", " + p2);

                    if (offscreenPointMaxCapturedTimeOutsideAllowedTimeSpanCount > 0)
                    {
                        log.Verbose("count of offscreen point (in smoothing set) capture times outside allowd time range from onscreen point (closest to corner): {0}/{1}", offscreenPointMaxCapturedTimeOutsideAllowedTimeSpanCount, offscreenSmoothingPoints.Count());
                        throw new InputDimensionException(string.Format("One or more offscreen frames  were outside the allowed buffer (try going slower)."));
                    }
                }

                result[corner] = offscreenSmoothingPoints.Select(o => o.Offscreen).Cast<SpatialPoint>();
            }

            return result;
        }

        private Dictionary<RectJointBoundary, CapturedInputPoint> GetCapturedPointsClosestToOnscreenIdeal(Dictionary<RectJointBoundary, PlanePoint> idealCornerPlanePoints)
        {
            var onscreenCornersClosestToIdeal = new Dictionary<RectJointBoundary, CapturedInputPoint>();

            foreach (RectJointBoundary corner in FourCorners)
            {
                log.Verbose("** Calculating data for corner '{0}' **", corner);

                PlanePoint idealCorner = idealCornerPlanePoints[corner];

                log.Verbose("Finding closest onscreen corner..");

                var touchPointsRankedByIncreasingDistanceToCorner = touchPoints.Select(
                    (p, i) => new
                    {
                        CapturedPoint = p,
                        OnscreenCornerPointDistance = p.Onscreen.DistanceFrom(idealCorner),
                        Index = i
                    }
                    ).OrderBy(
                        p => p.OnscreenCornerPointDistance
                        ).ToList();

                // Verify order is by increasing distance (does nothing)
                touchPointsRankedByIncreasingDistanceToCorner.Aggregate((p1, p2) =>
                {
                    Debug.Assert(p1.OnscreenCornerPointDistance <= p2.OnscreenCornerPointDistance);
                    return p2;
                });

                // Find nearest on-screen touched point 
                var cornerTouchPoint = touchPointsRankedByIncreasingDistanceToCorner.First();

                onscreenCornersClosestToIdeal[corner] = cornerTouchPoint.CapturedPoint;
            }

            return onscreenCornersClosestToIdeal;
        }

        private XYZRect GetSpatialRectFromOffscreenCornerPointSets(Dictionary<RectJointBoundary, IEnumerable<SpatialPoint>> offscreenCornerPointSets)
        {
            // all corners perpendiular
            // of: minimize deviation from square

            var lineSets = new RectLinePoints<SpatialPoint>
            {
                Top = offscreenCornerPointSets[RectJointBoundary.TopLeft].Concat(offscreenCornerPointSets[RectJointBoundary.TopRight]) ,
                Right = offscreenCornerPointSets[RectJointBoundary.TopRight].Concat(offscreenCornerPointSets[RectJointBoundary.BottomRight]),
                Bottom = offscreenCornerPointSets[RectJointBoundary.BottomRight].Concat(offscreenCornerPointSets[RectJointBoundary.BottomLeft]),
                Left = offscreenCornerPointSets[RectJointBoundary.BottomLeft].Concat(offscreenCornerPointSets[RectJointBoundary.TopLeft])
            };

            /////////////////////////////////////////////////////// CAUTION: emulation for test purposes
            if (IsOffscreenPointCaptureToBeEmulated)
                lineSets = enumalatedOffScreenPoints;
            //////////////////////////////////////////////

            //IEnumerable<SpatialPoint> allPoints = lineSets.AllPoints;//Values.Aggregate((l1, l2) => l1.Concat(l2));
            //SpatialPlane plane = GeometryExpert.GetPlaneFromPoints(allPoints);

            IEnumerable<SpatialPoint> planeFitPoints = offscreenCornerPointSets.Values.Aggregate(
                (l1,l2) => l1.Concat(l2) 
                ); //lineSets.Concatenated.Distinct();
            SpatialPlane plane = GeometryExpert.GetPlaneFromPoints(planeFitPoints);

            RectLinePoints<ProjectedSpatialPoint> projectedLinesPoints = lineSets.Apply(p => GeometryExpert.ProjectOntoPlane(p, plane));

            return GeometryExpert.GetSpatialRectangleFromSpatialLinePoints(plane, projectedLinesPoints);
        }

        private double GetRectangleDiagonalLength(Rect r)
        {
            return GeometryExpert.Euclidean(0, r.Width + r.Height);
        }

        private static Rect GetMaximizedRectSpannedByPlanePoints(IEnumerable<PlanePoint> touchPoints)
        {
            PlanePoint leftMost = null, topMost = null, rightMost = null, bottomMost = null;

            foreach (PlanePoint p in touchPoints)
            {
                if (leftMost == null || leftMost.X > p.X)
                    leftMost = p;

                if (topMost == null || topMost.Y > p.Y)
                    topMost = p;

                if (rightMost == null || rightMost.X < p.X)
                    rightMost = p;

                if (bottomMost == null || bottomMost.Y < p.Y)
                    bottomMost = p;
            }

            try { 
                var onscreenRect = new Rect(
                    new Point(
                        leftMost.X,
                        topMost.Y
                        ),
                    new Size(
                        rightMost.X - leftMost.X,
                        bottomMost.Y - topMost.Y
                        )
                    );
                return onscreenRect;
            }
            catch (Exception e)
            {
                throw new InputDimensionException("Error during rect maximization by plane points  (all corners may not be available)", e);

            }

        }

        private PlanePoint FindClosest(PlanePoint targetPoint, IEnumerable<PlanePoint> points)
        {
            return OrderByDistanceIncreasing(
                targetPoint, points
                ).First();
        }

        private static IEnumerable<PlanePoint> OrderByDistanceIncreasing(PlanePoint targetPoint, IEnumerable<PlanePoint> points)
        {
            return (
                from p in points select new { Point = p, Distance = p.DistanceFrom(targetPoint) }
                ).OrderBy(
                    pointDistance => pointDistance.Distance
                    ).Select(
                        pointDistance => pointDistance.Point
                        );
        }


        //private static T1 GetNearestNeighbour<T1, T2>(IEnumerable<T1> points, T2 targetPoint) where T1 : NeighbourDistance<T2>
        //{
        //    return GetKNearestNeighbours(points, targetPoint, 1).First();
        //}

        //private static IEnumerable<T1> GetKNearestNeighbours<T1, T2>(IEnumerable<T1> points, T2 targetPoint, int kNeighbourCount) where T1 : NeighbourDistance<T2>
        //{
        //    var skod = (
        //        from p in points select new { Point = p, Distance = p.DistanceFrom(targetPoint) }
        //        ).OrderBy(
        //            pointDistance => pointDistance.Distance
        //            ).ToList();

        //    var kClosest = (
        //        from p in points select new { Point = p, Distance = p.DistanceFrom(targetPoint) }
        //        ).OrderBy(
        //            pointDistance => pointDistance.Distance
        //            ).Select(
        //                pointDistance => pointDistance.Point
        //                ).Take(kNeighbourCount);// ToList();//.Sort((p1, p2) => p1.Distance.CompareTo(p2.Distance));

        //    return kClosest;
        //}

        // Render ink strokes as cubic bezier segments.

        private void RenderAllStrokes()
        {
            // Clear the canvas.
            InkCanvas.Children.Clear();

            // Get the InkStroke objects.
            IReadOnlyList<InkStroke> inkStrokes = inkManager.GetStrokes();

            // Process each stroke.
            foreach (InkStroke inkStroke in inkStrokes)
            {
                PathGeometry pathGeometry = new PathGeometry();
                PathFigureCollection pathFigures = new PathFigureCollection();
                PathFigure pathFigure = new PathFigure();
                PathSegmentCollection pathSegments = new PathSegmentCollection();

                // Create a path and define its attributes.
                Path path = new Path();
                path.Stroke = new SolidColorBrush(Colors.Red);
                path.StrokeThickness = STROKE_THICKNESS;

                // Get the stroke segments.
                IReadOnlyList<InkStrokeRenderingSegment> segments;
                segments = inkStroke.GetRenderingSegments();

                // Process each stroke segment.
                bool first = true;
                foreach (InkStrokeRenderingSegment segment in segments)
                {
                    // The first segment is the starting point for the path.
                    if (first)
                    {
                        pathFigure.StartPoint = segment.BezierControlPoint1;
                        first = false;
                    }

                    // Copy each ink segment into a bezier segment.
                    BezierSegment bezSegment = new BezierSegment();
                    bezSegment.Point1 = segment.BezierControlPoint1;
                    bezSegment.Point2 = segment.BezierControlPoint2;
                    bezSegment.Point3 = segment.Position;

                    // Add the bezier segment to the path.
                    pathSegments.Add(bezSegment);
                }

                // Build the path geometerty object.
                pathFigure.Segments = pathSegments;
                pathFigures.Add(pathFigure);
                pathGeometry.Figures = pathFigures;

                // Assign the path geometry object as the path data.
                path.Data = pathGeometry;

                // Render the path by adding it as a child of the Canvas object.
                InkCanvas.Children.Add(path);
            }
        }

        //private static double Distance(Point currentContact, Point previousContact)
        //{
        //    return Math.Sqrt(
        //        Math.Pow(currentContact.X - previousContact.X, 2) +
        //        Math.Pow(currentContact.Y - previousContact.Y, 2)
        //        );
        //}


        public IEnumerable<RectJointBoundary> FourCorners
        {
            get { return new List<RectJointBoundary> { RectJointBoundary.TopLeft, RectJointBoundary.TopRight, RectJointBoundary.BottomLeft, RectJointBoundary.BottomRight }; }
        }

        public bool IsOffscreenPointCaptureToBeEmulated { get { return IsOffscreenPointCaptureToBeEmulatedCheckBox.IsChecked.Value; } }

        #endregion
        #region Inner classes

        private class InputDimensionException : Exception
        {
            public InputDimensionException(string message) : base(message) { }

            public InputDimensionException(string message, Exception e) : base(message, e) { }
        }

        #endregion
    }
}
