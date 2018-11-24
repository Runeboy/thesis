using System;
using Airswipe.WinRT.Core;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Airswipe.WinRT.Core.Log;
using Windows.UI.Xaml;
using Airswipe.WinRT.Core.Data;
using System.Collections.Generic;
using Airswipe.WinRT.Core.Data.Dto;
using Airswipe.WinRT.UI.Controls;
using Windows.Storage;
using System.Linq;
using Accord.Statistics.Analysis;

namespace Airswipe.WinRT.UI.Pages
{
    public sealed partial class TestPage : BasicPage
    {
        #region Fields

        ILogger log = new TypeLogger<TestPage>();

        TrialSession TrialSession { get { return TrialSessions[SessionFilenameComboBoxBox.SelectedIndex]; } }

        List<TrialSession> TrialSessions = new List<TrialSession>();

        const int LOWER_TICK_VALUE = -AppSettings.UPPER_TICK_VALUE;
        const int TICK_COUNT = 2 * AppSettings.UPPER_TICK_VALUE + 1;

        public const int TICK_DISTANCE_MM = 10;
        public const double TICK_DISTANCE_PIXELS = AppSettings.DISPLAY_PIXELS_PER_MILLIMETER * TICK_DISTANCE_MM;

        const double TICK_FONT_SIZE = 35;
        const int TICK_LENGTH = 30;
        const double TICK_LINE_SIZE = 3;
        //const double MAX_TICK_LINE_SIZE = 10;
        const double TICK_LINE_SIZE_ORIGO_DISTANCE_MULTIPLIER = 1.065;
        const double TICK_LABEL_ELLIPSE_PADDING = 3;


        private int trialIndex;
        //private const int MIN_TARGET_VALUE = 10;

        StorageFolder trialsFolder;
        private int previousSelectedIndex = -1;

        //Exp3PointMapper exp3PCA = null;

        #endregion
        #region Constructors 

        public TestPage()
        {
            InitializeComponent();

            PrintOffscrenRectCornersWRTDevice();

            LogOffscreenRect();

            //SetMapLocationInputDimensionScale();
            ShowTrialInfoBarOnLeftEdgeTap();

            Loaded += async (a, b) =>
            {
                trialsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("trials", CreationCollisionOption.OpenIfExists);

                var files = await trialsFolder.GetFilesAsync();
                foreach (var file in files)
                {
                    string json = await FileIO.ReadTextAsync(file);
                    TrialSession session = StringExpert.FromJson<TrialSession>(json);
                    TrialSessions.Add(session);

                    SessionFilenameComboBoxBox.Items.Add(session.Type + ": " + file.DisplayName);
                }

                if (TrialSessions.Count == 0)
                {
                    //StartTrialAndInitializeMap(CurrentTrial);
                    StartNewTrialSession("unnamed", TrialSessionType.E1);
                }
                //else
                //    SessionFilenameComboBoxBox.SelectedIndex = 0;

                MasterPage.OffscreenReleaseByKeyDown += () =>
                {
                    if (!CurrentTrial.IsCompleted && CurrentTrial.MoveBullsEyeInsteadOfMap)
                        EndCurrentTrialAndMaybeAdvance(true);
                };

            };

            EndAndPersistTrialOnBullsEyeHoldAchieved();

            UpdateTrialSessionOnNameChange();

            Map.ProjectionRelease += () =>
            {
                if (!CurrentTrial.IsCompleted)
                    CurrentTrial.NotifySpaceReleased(CurrentTrial.ReleasePositionValue);
            };
            Map.ScreenRelease += () =>
            {
                if (!CurrentTrial.IsCompleted && !CurrentTrial.IsOffscreenSpaceEnabled)
                    CurrentTrial.NotifySpaceReleased(CurrentTrial.ReleasePositionValue);
            };

            Map.UIChange += () =>
            {
                if (!CurrentTrial.IsCompleted)
                {
                    CurrentTrial.FromTargetToReleasePosition = Map.FromTargetToReleasePosition;
                    CurrentTrial.InertialDistanceTraveled = Map.InertialDistanceTraveled;
                    CurrentTrial.NoninertialDistanceTraveled = Map.NoninertialDistanceTraveled;
                    CurrentTrial.TouchCount = Map.TouchCount;
                    CurrentTrial.OvershootTravelDistance = Map.OvershootTravelDistance;
                    CurrentTrial.BullsEyeEntryCount = Map.BullsEyeEntryCount;
                }

                UpdateCurrentTrialAndSessionUI();
            };

        }

        private void PrintOffscrenRectCornersWRTDevice()
        {
            var rect = AppSettings.InputSpace.Offscreen;
            var x = rect.OrigoToRight.Length;
            var y = rect.OrigoToBottom.Length;
            log.Info(
                "Offscreen rect corners WRT device: (TL:-{0},-{1}), (TR:{0},-{1}), (BR:{0},{1}), (BL:-{0},{1})",
                x,y
                //rect[RectJointBoundary.TopLeft].Subtract(rect.Origo).ToShortString(),
                //rect[RectJointBoundary.TopRight].Subtract(rect.Origo).ToShortString(),
                //rect[RectJointBoundary.BottomRight].Subtract(rect.Origo).ToShortString(),
                //rect[RectJointBoundary.BottomLeft].Subtract(rect.Origo).ToShortString(),
            );
        }

        private void LogOffscreenRect()
        {
            var PointShortFormat = new Func<PointComponents, string>(p => "(" + StringExpert.CommaSeparate(p.Components) + ")");
            log.Info("Offscreen rect: {0} -- {1} -- {2} -- {3}",
                PointShortFormat(AppSettings.InputSpace.Offscreen[RectJointBoundary.TopLeft]),
                PointShortFormat(AppSettings.InputSpace.Offscreen[RectJointBoundary.TopRight]),
                PointShortFormat(AppSettings.InputSpace.Offscreen[RectJointBoundary.BottomLeft]),
                PointShortFormat(AppSettings.InputSpace.Offscreen[RectJointBoundary.BottomRight])
                );
        }

        private int TrialIndex
        {
            get { return trialIndex; }
            set
            {
                if (value < 0)
                    return;
                //Map.Dispatcher.RunAsync(
                //    new Action(() => { })
                //    );
                //Map.Visibility = Visibility.Collapsed;
                //Dispatcher.ProcessEvents(Windows.UI.Core.CoreProcessEventsOption.ProcessAllIfPresent);
                //Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);



                trialIndex = value;

                InitializeMapFromTrial(TrialSession, CurrentTrial);

                //Map.Visibility = Visibility.Visible;

                if (TrialComboBox.SelectedIndex != value)
                    TrialComboBox.SelectedIndex = value;


                UpdateCurrentTrialAndSessionUI();
            }
        }

        private void UpdateCurrentTrialAndSessionUI()
        {
            if (SessionFilenameNameTextBox.Text != TrialSession.Name)
                SessionFilenameNameTextBox.Text = TrialSession.Name;

            //TrialFilenameTextBox.Text = TrialSession.Filename;

            CurrentTrialCommentTextBox.Text = CurrentTrial.Comment;

            TrialDescriptionTextBlock.Text = string.Format(
                "{0}/{1} \r\n{2}",
                (TrialSession.Trials.IndexOf(CurrentTrial) + 1),
                TrialSession.Trials.Count,
                CurrentTrial.ToJson()
                );

            UpdateTrialIndexButtonEnabled();
        }

        //private Rect? bullsEyeRect2;

        //Rect mapContainerRect = Rect.Empty;
        //Rect mapRect = Rect.Empty;

        private void UpdateTrialSessionOnNameChange()
        {
            SessionFilenameNameTextBox.TextChanged += async (s, e) =>
            {
                string newName = (s as TextBox).Text;
                bool isChange = (TrialSession.Name != newName);
                if (!isChange)
                    return;

                IStorageItem file = await trialsFolder.TryGetItemAsync(TrialSession.Filename);
                if (file != null)
                    file.DeleteAsync();

                TrialSession.Name = newName;
                //SessionFilenameComboBoxBox.Items[SessionFilenameComboBoxBox.SelectedIndex] = newName;

                SaveTrialSession();
            };
        }

        private void EndAndPersistTrialOnBullsEyeHoldAchieved()
        {
            Map.TargetHoldAchieved += async () =>
            {
                EndCurrentTrialAndMaybeAdvance(false);
            };
        }

        private async void EndCurrentTrialAndMaybeAdvance(bool isForcefullyEnded)
        {
            CurrentTrial.NotifySpaceReleased(CurrentTrial.ReleasePositionValue);
            if (CurrentTrial.IsOffscreenSpaceEnabled)
                CurrentTrial.FinalOffscreenLocation = Map.LastOffscreenProjection.Source;

            CurrentTrial.End(isForcefullyEnded);

            Map.IsMapMoveEnabled = false;

            Map.MapMoveStatus = MapControl.MapMoveStatuses.NotMoving;

            await SaveTrialSession();

            var dialog = new Windows.UI.Popups.MessageDialog(
                string.Format("Trial ended ({0}% complete).", (int)(TrialSession.CompletionRate * 100))
                );
            await dialog.ShowAsync();

            //TrialSession.EndTrialAndAdvance();

            bool isLastTrial = (TrialIndex == TrialSession.Trials.Count - 1);
            if (!isLastTrial)
            {
                TrialIndex++;
                //InitializeMapFromTrial(CurrentTrial);
            }
            else {
                await new Windows.UI.Popups.MessageDialog("Last trial reached.").ShowAsync();
            }
        }

        private async System.Threading.Tasks.Task SaveTrialSession()
        {
            var file = await trialsFolder.CreateFileAsync(TrialSession.Filename, CreationCollisionOption.ReplaceExisting);

            string s = TrialSession.ToJson();

            log.Verbose("writing trial session to file: " + file.Path);
            await FileIO.WriteTextAsync(file, TrialSession.ToJson());

            UpdateCurrentTrialAndSessionUI();

        }

        private void StartNewTrialSession(string name, TrialSessionType type)
        {
            var trialSession = new TrialSession(name, type: type) { Space = AppSettings.InputSpace };

            TrialSessions.Add(trialSession);
            SessionFilenameComboBoxBox.Items.Add(trialSession.Filename);

            SessionFilenameComboBoxBox.SelectedIndex = SessionFilenameComboBoxBox.Items.Count - 1;

            //UpdateCurrentTrialAndSessionUI();
        }

        #endregion
        #region Event handlers

        private void HideTrialBarButton_Click(object sender, RoutedEventArgs e)
        {
            TrialInfoPopup.IsOpen = false;
        }

        private void NewTrialBarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TrialSession.Trials.Insert(0, new Trial
                {
                    Mode = (TrialMode)Enum.Parse(typeof(TrialMode), (string)NewTrialTypeComboBox.SelectedValue),
                    TargetValue = int.Parse(NewTrialTargetValueTextBox.Text),
                    Comment = NewTrialCommentTextBox.Text,
                    IsOffscreenSpaceEnabled = NewTrialEnabledOffscreenMoveCheckBox.IsChecked.Value
                });

                InitializeMapFromTrial(TrialSession, CurrentTrial);
            }
            catch (Exception)
            {
                new Windows.UI.Popups.MessageDialog("One or more values were unspecified.").ShowAsync();
            }
        }

        private async void InitializeMapFromTrial(TrialSession session, Trial trial)
        {
            Map.Visibility = Visibility.Visible;

            Map.Reset();

            //trial.Start();

            TargetValueTextBlock.Text = trial.TargetValue.ToString();
            TargetValueEllipse.Fill = new SolidColorBrush(MapControl.GetTickBackgroundColor(trial.TargetValue));
            //TrialDescriptionTextBlock.Text = trial.ToJson();

            //log.Verbose("trial started");
            TrialNameTextBlock.Text = trial.Name;
            //InitializeMap(trial);
            //Point mapContainerCenter = new Point(mapContainerSize.Width, mapContainerSize.Height);

            double sideBufferWidth = Map.Width * (3 / 4.0); // maps stops when the last tick mark is in the middle of the screen 
            double mapSize = (TICK_COUNT + 1) * TICK_DISTANCE_PIXELS + 2 * sideBufferWidth;

            log.Info("\nMap square size:\n\tpixels: {0}\n" +
                        "\tmm: {1}\n" +
                     "Map container size:\n" +
                        "\tpixels: ({2},{3})\n" +
                            "\tmm: ({4},{5})\n" +
                    "Tick distance (pixels): {8}\n " +
                    "Tick distance (mm): {6}\n " +
                    "Bulls eye size: {7}",
                mapSize,
                AppSettings.OnscreenDistanceToMillimeter(mapSize),
                Map.Width,
                Map.Height,
                AppSettings.OnscreenDistanceToMillimeter(Map.Width),
                AppSettings.OnscreenDistanceToMillimeter(Map.Height),
                AppSettings.OnscreenDistanceToMillimeter(TICK_DISTANCE_PIXELS),
                AppSettings.OnscreenDistanceToMillimeter(Map.BullsEyeSize),
                TICK_DISTANCE_PIXELS
                );


            Map.MapSize = new Size(mapSize, mapSize);

            Map.MapChildren = getTrialModeUIELements(Map.MapCanvasCenter.X);



            Map.IsHorsontalMapMoveAllowed =
                //!trial.IsValuesToBeEstimated && 
                (
                    trial.Mode == TrialMode.Horisontal ||
                    trial.Mode == TrialMode.TwoDimensional
                );
            Map.IsVerticalMapMoveAllowed =
                //!trial.IsValuesToBeEstimated && 
                (
                    trial.Mode == TrialMode.Vertical ||
                    trial.Mode == TrialMode.TwoDimensional
                );
            Map.IsOffscreenSpaceEnabled = trial.IsOffscreenSpaceEnabled;
            Map.MapMoveProjectionMode = trial.ProjectionMode;
            //InitializeInputCanvas(mapSize, mapContainerSize);
            //InitializeBullsEye(mapContainerSize);

            Map.MoveBullsEyeInsteadOfMap = trial.MoveBullsEyeInsteadOfMap;

            //FileSavePicker picker = new FileSavePicker();
            //picker.FileTypeChoices.Add("JPEG Image", new string[] { ".jpg" });

            //StorageFile file = await picker.PickSaveFileAsync();

            //Rect r = new Rect(new Point(0, 0), new Point(Map.Width, Map.Height));
            //foreach (var c in Map.Children)
            //    c.Clip = new RectangleGeometry() { Rect = r };

            //RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            //await renderTargetBitmap.RenderAsync(Map, (int)Map.Width, (int)Map.Height);

            //Map.MapChildren = new List<FrameworkElement> { new Image { Source = renderTargetBitmap } };

            Map.OptimizeByConvertMapChildrenToSingleImage();
            //if (file != null)
            //{
            //    var pixels = await renderTargetBitmap.GetPixelsAsync();

            //    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            //    {
            //        var encoder = await
            //        BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
            //        byte[] bytes = pixels.ToArray();
            //        encoder.SetPixelData(BitmapPixelFormat.Bgra8,
            //            BitmapAlphaMode.Ignore,
            //            (uint)Map.Width, (uint)Map.Height,
            //            96, 96, bytes);

            //        await encoder.FlushAsync();
            //    }
            //}

            bool isToPreProcessProjections = CurrentTrial.IsExp3SpaceToBeUsed; //(session.Type == TrialSessionType.E3);
            if (isToPreProcessProjections)
            {
                Map.ProjectionPreProcess = (p => DoExp3ProjectionPreProcess(p));
                var skod = Exp3PCA;
            }
            else
                Map.ProjectionPreProcess = null;

            trial.FromOrigoToTargetPosition = new Point(
                Map.Target.X - Map.MapCanvasCenter.X,
                Map.Target.Y - Map.MapCanvasCenter.Y
                );
            log.Info("Will projections be pre-processed: " + (isToPreProcessProjections ? "YES" : "NO"));
        }

        #endregion
        #region Methods

        private void DoExp3ProjectionPreProcess(PlanePoint p)
        {
            //double[][] transformed = Exp3PCA.Transform(new double[][] { new double[] { p.X, p.Y } });
            //p.X = transformed[0][0];
            //p.Y = transformed[0][1];
            var transformed = Exp3PCA.FromModeledToNormalSpace(p);
            p.X = transformed.X;
            p.Y = transformed.Y;
        }

        private Size ActualSize(DependencyObject element)
        {
            return new Size(
                (double)element.GetValue(ActualWidthProperty),
                (double)element.GetValue(ActualHeightProperty)
                );
        }

        private IEnumerable<FrameworkElement> getTrialModeUIELements(double origo)
        {
            switch (CurrentTrial.Mode)
            {
                case TrialMode.Horisontal: return CreateOneDimensionalTickMarks(origo, true);
                case TrialMode.Vertical: return CreateOneDimensionalTickMarks(origo, false);
                case TrialMode.TwoDimensional: return Create2DTickMarks(origo, false);
                    //case TrialMode.Estimation: return Create2DTickMarks(origo, false);
            }
            throw new NotImplementedException("Cannot handle mode.");
        }

        private double getTickLineSizeMultiplier(double tickValue)
        {
            return Math.Pow(TICK_LINE_SIZE_ORIGO_DISTANCE_MULTIPLIER, Math.Abs(tickValue));
        }

        private IEnumerable<FrameworkElement> CreateOneDimensionalTickMarks(double origo, bool isHorisontal)
        {
            for (int tickValue = LOWER_TICK_VALUE; tickValue <= AppSettings.UPPER_TICK_VALUE; tickValue++)
            {
                //if (!isHorisontal)
                //    tickValue = -tickValue;
                //var tickValue = -upperTickValue -1 +i;

                //var x = i * tickDistance + sideBufferWidth;
                double tickAxisLocation = origo + TICK_DISTANCE_PIXELS * (isHorisontal ? tickValue : -tickValue); //i * tickDistance + sideBufferWidth;
                var tickPosition = new Point(
                    isHorisontal ? tickAxisLocation : origo,
                    isHorisontal ? origo : tickAxisLocation
                    );

                double sizeMultiplier = getTickLineSizeMultiplier(tickValue);

                double tickLineStartLocation = origo - TICK_LENGTH / 2.0 * sizeMultiplier;
                double tickLineEndLocation = origo + TICK_LENGTH / 2.0 * sizeMultiplier;
                bool isTickValueTarget = tickValue == (CurrentTrial.TargetValue);

                if (isTickValueTarget)
                    Map.Target = tickPosition;

                Brush tickBrush = new SolidColorBrush(isTickValueTarget ?
                    MapControl.TICK_TARGET_COLOR :
                    MapControl.GetTickBackgroundColor(tickValue)
                    );

                var line = new Line
                {
                    X1 = isHorisontal ? tickAxisLocation : tickLineStartLocation,
                    Y1 = isHorisontal ? tickLineStartLocation : tickAxisLocation,
                    X2 = isHorisontal ? tickAxisLocation : tickLineEndLocation,
                    Y2 = isHorisontal ? tickLineEndLocation : tickAxisLocation,
                    StrokeThickness = TICK_LINE_SIZE * sizeMultiplier,
                    Stroke = tickBrush
                };
                //MapCanvas.Children.Add(line);

                var labelTextBlock = new TextBlock
                {
                    Text = tickValue.ToString(),
                    Foreground = new SolidColorBrush(MapControl.TICK_FONT_COLOR),
                    FontSize = TICK_FONT_SIZE,
                };
                //var labelAxisLocation = 

                //MapCanvas.Children.Add(t);

                labelTextBlock.Measure(Size.Empty);

                //var labelOffSet = new Point(
                //    labelTextBlock.ActualWidth / 2.0, 
                //    labelTextBlock.ActualHeight / 2.0
                //    );
                var labelRect = new Rect(
                    new Point(
                        tickPosition.X - labelTextBlock.ActualWidth / 2.0,
                        tickPosition.Y - labelTextBlock.ActualHeight / 2.0
                    ),
                    new Size(labelTextBlock.ActualWidth, labelTextBlock.ActualHeight)
                    );

                Canvas.SetLeft(labelTextBlock, labelRect.X);
                //Canvas.SetTop(labelTextBlock, isHorisontal ? tickLineEndLocation + 3 : tickAxisLocation - 3);
                Canvas.SetTop(labelTextBlock, labelRect.Y);


                double shortSideWidth = (isHorisontal ? labelRect.Width : labelRect.Height) + 2 * TICK_LABEL_ELLIPSE_PADDING;
                double longSideWidth = ((isHorisontal ? labelRect.Height : labelRect.Width) + 2 * TICK_LABEL_ELLIPSE_PADDING) * sizeMultiplier;

                var labelEllipse = new Ellipse
                {
                    Width = isHorisontal ? shortSideWidth : longSideWidth,
                    Height = isHorisontal ? longSideWidth : shortSideWidth,
                    //StrokeThickness = TICK_LINE_SIZE * sizeMultiplier,
                    //Stroke = new SolidColorBrush(isTickValueTarget ? TICK_TARGET_COLOR : TICK_COLOR),
                    Fill = tickBrush
                };


                Canvas.SetLeft(labelEllipse, tickPosition.X - labelEllipse.Width / 2.0);
                Canvas.SetTop(labelEllipse, tickPosition.Y - labelEllipse.Height / 2.0);

                // return in increasing level of layers
                //   yield return line;
                yield return labelEllipse;
                yield return labelTextBlock;
            }
        }

        private IEnumerable<FrameworkElement> Create2DTickMarks(double origo, bool v)
        {

            //var tickValues = new List<int>();
            //for (int tickValue = LOWER_TICK_VALUE; tickValue <= AppSettings.UPPER_TICK_VALUE; tickValue++)
            //    tickValues.Add(tickValue);

            //double angleStep = Math.PI / (tickValues.Count + 2);
            //var angles = new List<double>();
            //for (double angle = angleStep; angle < Math.PI; angle += angleStep)
            //    angles.Add(angleStep);

            //RandomizeList(angles);

            //var tickInfos = tickValues.Select((int value, int index) => new
            //{
            //    value = value,
            //    angle = angles[index]
            //});

            //foreach(var tickInfo in tickInfos)
            //{

            //    var tickValue = tickInfo.value;
            //    var angle = Math.PI/4.0; //tickInfo.angle;
            for (int tickValue = LOWER_TICK_VALUE; tickValue <= AppSettings.UPPER_TICK_VALUE; tickValue++)
            {
                var angle = CurrentTrial.UIAngle;
                //var tickValue = -upperTickValue -1 +i;

                //var x = i * tickDistance + sideBufferWidth;
                //var tickAxisLocation = origo + TICK_DISTANCE * tickValue; //i * tickDistance + sideBufferWidth;

                double distanceFromOrigo = TICK_DISTANCE_PIXELS * tickValue;
                double sizeMultiplier = getTickLineSizeMultiplier(tickValue);
                bool isTickValueTarget = (tickValue == CurrentTrial.TargetValue);

                //var tickPosition = new Point(
                //    origo,
                //    origo - distanceFromOrigo
                //    );
                var tickPosition = new Point(
                    origo + Math.Cos(angle) * distanceFromOrigo,
                    origo - Math.Sin(angle) * distanceFromOrigo
                    );

                if (isTickValueTarget)
                    Map.Target = tickPosition;

                Brush tickBrush = new SolidColorBrush(isTickValueTarget ? MapControl.TICK_TARGET_COLOR : MapControl.GetTickBackgroundColor(tickValue));

                if (distanceFromOrigo >= 0)
                {
                    double circleLineThickness = TICK_LINE_SIZE * sizeMultiplier;
                    double circleSize = distanceFromOrigo * 2 + circleLineThickness / 2.0;
                    var circleEllipse = new Ellipse
                    {
                        Width = circleSize,
                        Height = circleSize,
                        StrokeThickness = circleLineThickness,
                        Stroke = new SolidColorBrush(MapControl.RING_COLOR) //tickBrush
                    };
                    Canvas.SetZIndex(circleEllipse, -1);
                    //setEllipseCenterPositionInCanvas(circleEllipse, origo, origo);
                    double circlePosition = origo - distanceFromOrigo - circleLineThickness / 2.0;
                    setPositionInCanvas(circleEllipse, circlePosition, circlePosition);

                    yield return circleEllipse;
                }
                //Canvas.SetLeft(circleEllipse, origo - distanceFromOrego) ;
                //Canvas.SetTop(circleEllipse, origo - distanceFromOrego);
                //MapCanvas.Children.Add(line);

                var labelTextBlock = new TextBlock
                {
                    Text = tickValue.ToString(),
                    Foreground = new SolidColorBrush(MapControl.TICK_FONT_COLOR),
                    FontSize = TICK_FONT_SIZE
                };
                labelTextBlock.Measure(Size.Empty);


                var labelRect = new Rect(
                    new Point(
                        tickPosition.X - labelTextBlock.ActualWidth / 2.0,
                        tickPosition.Y - labelTextBlock.ActualHeight / 2.0
                    ),
                    new Size(labelTextBlock.ActualWidth, labelTextBlock.ActualHeight)
                    );

                setPositionInCanvas(labelTextBlock, labelRect.X, labelRect.Y);

                var labelEllipseDims = new Point(
                    (labelRect.Width + 2 * TICK_LABEL_ELLIPSE_PADDING) * sizeMultiplier,
                    labelRect.Height + 2 * TICK_LABEL_ELLIPSE_PADDING
                    );
                var labelEllipse = new Ellipse
                {
                    Width = labelEllipseDims.X, // (labelRect.Width + 2 * TICK_LABEL_ELLIPSE_PADDING) * sizeMultiplier,
                    Height = labelEllipseDims.Y, // labelRect.Height + 2 * TICK_LABEL_ELLIPSE_PADDING,
                    //StrokeThickness = TICK_LINE_SIZE * sizeMultiplier,
                    //Stroke = new SolidColorBrush(isTickValueTarget ? TICK_TARGET_COLOR : TICK_COLOR),
                    Fill = tickBrush,
                    //RenderTransformOrigin = new Point(tickPosition.X, tickPosition.Y5),
                    RenderTransform = new RotateTransform()
                    {
                        Angle = 90 - GeometryExpert.RadianToDegree(angle),
                        CenterX = labelEllipseDims.X / 2.0,
                        CenterY = labelEllipseDims.Y / 2.0
                        //CenterX = tickPosition.X, CenterY = tickPosition.Y
                    },
                };
                setPositionInCanvas(labelEllipse, tickPosition.X - labelEllipse.Width / 2.0, tickPosition.Y - labelEllipse.Height / 2.0);
                //Canvas.SetLeft(labelTextBlock, labe);
                //Canvas.SetTop( labelTextBlock, origo - distanceFromOrego - TICK_FONT_SIZE / 2.0 - (tickValue == 0? 0 : TICK_FONT_SIZE / 2.0));
                //MapCanvas.Children.Add(t);

                yield return labelEllipse;
                yield return labelTextBlock;
            }
        }



        private void ShowTrialInfoBarOnLeftEdgeTap()
        {
            EdgeTap += (location) =>
            {
                if (location == RectLineBoundary.Left)
                    TrialInfoPopup.IsOpen = true;
            };
        }


        private static void setPositionInCanvas(FrameworkElement el, double x, double y)
        {
            Canvas.SetLeft(el, x);
            Canvas.SetTop(el, y);
        }



        //private void SetMapLocationInputDimensionScale()
        //{

        //}

        #endregion
        #region Properties

        public bool IsModeModeColoringEnabled
        {
            get { return Map.IsModeModeColoringEnabled; }
        }

        public string[] TrialModes
        {
            get { return Enum.GetNames(typeof(TrialMode)); }
        }

        public Rect MapRect
        {
            get
            {
                var space = AppSettings.InputSpace;
                return (space != null) ?
                    space.Onscreen :
                    new Rect(new Point(0, 0), new Size(Width, Height));
            }
        }

        public Trial CurrentTrial
        {
            get { return TrialSession.Trials[TrialIndex]; }
        }

        #endregion

        private void IsModeModeColoringEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Map.IsModeModeColoringEnabled = (sender as CheckBox).IsChecked.Value;
        }

        private void RestartTrialButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTrial.Restart();

            InitializeMapFromTrial(TrialSession, CurrentTrial);
            UpdateCurrentTrialAndSessionUI();
        }

        private void PreviousTrialButton_Click(object sender, RoutedEventArgs e)
        {
            TrialIndex--;
        }

        private void UpdateTrialIndexButtonEnabled()
        {
            PreviousTrialButton.IsEnabled = (TrialIndex > 0);
            NextTrialButton.IsEnabled = (TrialIndex < TrialSession.Trials.Count - 1);
        }

        private async void SaveTrialButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveTrialSession();

            var dialog = new Windows.UI.Popups.MessageDialog(
                string.Format("Session saved ({0}% complete).", (int)(TrialSession.CompletionRate * 100))
                );
            await dialog.ShowAsync();
        }

        private async void SetSessionSpaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (TrialSession.Space != null)
                await new Windows.UI.Popups.MessageDialog("Session space has already been set for this session.").ShowAsync();
            else
            {
                TrialSession.Space = AppSettings.InputSpace;
                await new Windows.UI.Popups.MessageDialog("Session space set, not saved.").ShowAsync();
            }
        }

        private void NextTrialButton_Click(object sender, RoutedEventArgs e)
        {
            TrialIndex++;
        }

        private void CurrentTrialCommentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CurrentTrial.Comment = (sender as TextBox).Text;
            SaveTrialSession();
        }

        private async System.Threading.Tasks.Task GetSessionNameAndCreateNew(TrialSessionType type)
        {
            //await SaveTrialSession();
            InputMessageDialog dialog = new InputMessageDialog("Name", "unnamed");
            if (await dialog.ShowAsync())
            {
                string name = dialog.Value.Trim();
                if (!string.IsNullOrEmpty(name))
                    StartNewTrialSession(name, type);
            }
        }

        private void Map_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        { 

        }

        private void Map_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!CurrentTrial.IsStarted)
            {
                CurrentTrial.Restart();
                UpdateCurrentTrialAndSessionUI();
            }
        }

        private void TrialFilenameComboBoxBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isChange = SessionFilenameComboBoxBox.SelectedIndex != previousSelectedIndex;
            if (!isChange)
                return;

            TrialComboBox.Items.Clear();
            foreach (Trial t in TrialSession.Trials)
                TrialComboBox.Items.Add(String.Format("{0}/{1} {2}", TrialSession.Trials.IndexOf(t) + 1, TrialSession.Trials.Count, t.Name));

            TrialIndex = TrialSession.Trials.IndexOf(TrialSession.FirstIncompleteTrial);

            previousSelectedIndex = SessionFilenameComboBoxBox.SelectedIndex;


        }

        private void TrialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TrialIndex = (sender as ComboBox).SelectedIndex;
        }

        private async void NewTrialButton_Click(object sender, RoutedEventArgs e)
        {
            await GetSessionNameAndCreateNew(TrialSessionType.E1);
        }

        private async void NewE2Button_Click(object sender, RoutedEventArgs e)
        {
            await GetSessionNameAndCreateNew(TrialSessionType.E2);
        }

        private async void NewE3Button_Click(object sender, RoutedEventArgs e)
        {
            await GetSessionNameAndCreateNew(TrialSessionType.E3);
        }

        private Exp3PointMapper Exp3PCA
        {
            get
            {
                if (AppSettings.Exp3PointRemapper == null)
                    throw new Exception("PCA data is not available (has E2 been completed and processed?).");


                //if (exp3PCA != null)
                //{
                //    var pcaData = AppSettings.Exp3PCAData;
                //    if (pcaData != null)
                //        throw new Exception("PCA data is not available (has E2 been completed and processed?).");

                //    exp3PCA = new Exp3PointMapper(pcaData);
                //}

                return AppSettings.Exp3PointRemapper;
            }
        }


        //public double BUllsEyeSizeSetting
        //{
        //    get {
        //        return AppSettings.MilliMeterToOnscreenDistance(AppSettings.BULLSEYE_RADIUS_MILLIMETERS);
        //    }
        //}
    }
}
