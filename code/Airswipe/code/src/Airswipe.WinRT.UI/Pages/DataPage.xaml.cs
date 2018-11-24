using Airswipe.WinRT.Core;
using Airswipe.WinRT.Core.Data.Dto;
using Airswipe.WinRT.Core.Log;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using System;
using Airswipe.WinRT.Core.Data;
using System.Linq;
using Airswipe.WinRT.Core.MotionTracking;
using System.Collections;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using System.Reflection;
using System.Data;
using Airswipe.WinRT.Core.Misc;
using Windows.Foundation;

namespace Airswipe.WinRT.UI.Pages
{
    public sealed partial class DataPage : BasicPage
    {
        #region Fields

        private static readonly ILogger log = new TypeLogger<DataPage>();

        List<TrialSession> TrialSessions = new List<TrialSession>();
        StorageFolder trialsFolder;
        StorageFolder csvFolder;
        private readonly string CsvNullValueString = "NULL";

        static Dictionary<PropertyInfo, string> propertyStringMappings = new Dictionary<PropertyInfo, string>();

        private const string MEAN_SESSION_NAME = "mean";
        private const string STD_SESSION_NAME = "std";
        private const string OUTLIER_POSTFIX = "IsOutlier";

        class SelectableSession
        {
            public string Name { get { return Session.Name; } }
            public bool IsChecked { get; set; }
            public TrialSession Session { get; set; }
        }

        class PropertyStat
        {
            public double mean;
            public double std;
            public double confidence;
        }

        class StatsGroup : List<Trial>
        {
            public StatsGroup(IEnumerable<Trial> trials)
            {
                AddRange(trials);
            }

            public TrialSessionType SessionType { get; set; }
            public double UIAngle { get; set; }
            public string GroupName { get; set; }
            public ProjectionMode ProjectionMode { get; set; }
            public TrialMode TrialMode { get; set; }
            public bool IsEstimationMode { get; set; }
            public double TargetValue { get; set; }
            public Point TargetLocation { get; set; }
            //public PlanePoint Exp3TargetPosition { get; set; }
            public PlanePoint Exp3ReleasePosition { get; set; }

            public bool IsExp3SpaceToBeUsed { get; set; }

            public Dictionary<PropertyInfo, PropertyStat> PropertyStats = new Dictionary<PropertyInfo, PropertyStat>();
            //public Dictionary<PropertyInfo, double> Confidences = new Dictionary<PropertyInfo, double>();
            //public Dictionary<PropertyInfo, double> Std = new Dictionary<PropertyInfo, double>();
            //double angle { get; set; }
        }

        #endregion
        #region Constructors 

        static DataPage()
        {
            foreach (var sessionProp in SessionProps)
                propertyStringMappings[sessionProp] = "session" + sessionProp.Name;

            //table["session" + sessionProp.Name] = new List<object>();
            //table.Columns.Add("session" + sessionProp.Name);
            foreach (var trialProp in TrialProps)
                propertyStringMappings[trialProp] = "trial" + trialProp.Name;
            //table["trial" + trialProp.Name] = new List<object>();
            //table.Columns.Add("trial" + trialProp.Name);

        }

        public DataPage()
        {
            InitializeComponent();

            Loaded += async (a, b) =>
            {
                trialsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("trials", CreationCollisionOption.OpenIfExists);
                csvFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("csv", CreationCollisionOption.OpenIfExists);

                var files = await trialsFolder.GetFilesAsync();
                foreach (var file in files)
                {
                    string json = await FileIO.ReadTextAsync(file);
                    var trialSession = StringExpert.FromJson<TrialSession>(json);
                    TrialSessions.Add(trialSession);

                    Sessions2ItemsControl.Items.Add(new SelectableSession { Session = trialSession });
                    SessionFilenameComboBox.Items.Add(
                        string.Format("{0} ({1}, {2}%)", file.DisplayName, trialSession.Type, (int)(trialSession.CompletionRate * 100))
                        );
                }
            };

        }

        #endregion
        #region 

        private void TrialFilenameComboBoxBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //bool isChange = SessionFilenameComboBoxBox.SelectedIndex != previousSelectedIndex;
            //if (!isChange)
            //    return;



            SessionsItemsControl.Items.Clear();
            foreach (Trial t in SelectedSession.Trials)
                SessionsItemsControl.Items.Add(t);
            //SessionsItemsControl.Items.Add(string.Format("{0}/{1} {2}", SelectedSession.Trials.IndexOf(t) + 1, SelectedSession.Trials.Count, t.Name));

            //TrialIndex = TrialSession.Trials.IndexOf(TrialSession.FirstIncompleteTrial);

            //previousSelectedIndex = SessionFilenameComboBoxBox.SelectedIndex;

        }

        #endregion

        #region Properties

        public TrialSession SelectedSession
        {
            get { return TrialSessions[SessionFilenameComboBox.SelectedIndex]; }
        }

        public IList AllTrialModes
        {
            get { return Enum.GetValues(typeof(TrialMode)); }
        }

        public IEnumerable<ProjectionMode> NonBaselineProjectionModes
        {
            get
            {
                return new List<ProjectionMode> { ProjectionMode.Directional, ProjectionMode.PlaneNormal, ProjectionMode.Spherical };
            }
        }


        #endregion

        private bool IsSessionSelected { get { return SessionFilenameComboBox.SelectedIndex >= 0; } }

        public IEnumerable<TrialSession> SelectedSessions
        {
            get { return Sessions2ItemsControl.Items.Cast<SelectableSession>().Where(i => i.IsChecked).Select(i => i.Session); }
        }

        public static IEnumerable<PropertyInfo> SessionProps
        {
            get { return typeof(TrialSession).GetRuntimeProperties().Where(p => !PropHasAttribute<CsvIgnoreAttribute>(p)).ToList(); }
        }

        public static IEnumerable<PropertyInfo> TrialProps
        {
            get { return typeof(Trial).GetRuntimeProperties().Where(p => !PropHasAttribute<CsvIgnoreAttribute>(p)).ToList(); }
        }

        public static IEnumerable<PropertyInfo> TrialStatsProps
        {
            get { return typeof(Trial).GetRuntimeProperties().Where(p => PropHasAttribute<CsvDoStatsAttribute>(p)).ToList(); }
        }

        private static IEnumerable<PropertyInfo> GetProps<T, A>() where A : Attribute
        {
            return typeof(T).GetRuntimeProperties().Where(p => PropHasAttribute<A>(p));
        }

        public static IEnumerable<PropertyInfo> TrialPCAProps
        {
            get { return typeof(Trial).GetRuntimeProperties().Where(p => PropHasAttribute<CsvPcaXAttribute>(p)).ToList(); }
        }

        private static bool PropHasAttribute<A>(PropertyInfo p) where A : Attribute
        {
            return p.GetCustomAttributes(typeof(A)).Count() > 0;
        }

        private static A GetAttribute<A>(PropertyInfo p) where A : Attribute
        {
            return (A)p.GetCustomAttributes(typeof(A)).First();
        }

        private async void EstimationsButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!IsSessionSelected)
                return;

            //var session = SelectedSession;
            //var filePrefix = session.Name;
            //var trials = session.Trials;

            await WriteTrialsEstimations(new List<TrialSession> { SelectedSession });
        }

        private async Task WriteTrialsEstimations(IEnumerable<TrialSession> sessions)
        {
            if (sessions.Count() == 0)
            {
                return;
            }

            string filePrefix = "(" + sessions.Select(s => s.Name).Aggregate((n1, n2) => n1 + "," + n2) + ")";


            var columns = new List<string>
            {
                "targetValue",
                "releasePositionValue",
                "releasePositionTargetDiff",
                //"releaseDistanceFromTargetValueX", 
                //"releaseDistanceFromTargetValueY", 
                "sessionName"
            };

            string csvPattern = columns.Select((c, i) => "{" + i + "}").Aggregate((s1, s2) => s1 + "\t" + s2); //"{0}\t{1}\t{2}\r\n";
            string header = string.Format(csvPattern, columns.ToArray());

            int fileCount = 0;

            foreach (ProjectionMode projectionMode in NonBaselineProjectionModes)
                foreach (TrialMode trialMode in AllTrialModes)
                {
                    var lines = new List<string> { header };

                    string filename = string.Format("Estimations--{0}--{1}--{2}.csv", filePrefix, projectionMode, trialMode);

                    foreach (TrialSession session in sessions)
                    {
                        var filteredTrials = session.Trials.Where(t =>
                                t.MoveBullsEyeInsteadOfMap && // = estimation exercise
                                t.ProjectionMode == projectionMode &&
                                t.Mode == trialMode
                                );
                        foreach (Trial trial in filteredTrials)
                        {
                            //var distance = 
                            lines.Add(string.Format(csvPattern,
                                trial.TargetValue,
                                trial.ReleasePositionValue,
                                //GeometryExpert.Euclidean(trial.FromTargetToReleasePosition.X, trial.FromTargetToReleasePosition.Y),
                                //trial.FromTargetToReleasePosition.X,
                                //trial.FromTargetToReleasePosition.Y,
                                trial.ReleasePositionValue - trial.TargetValue,
                                session.Name
                                ));
                        }
                    }
                    var file = await csvFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteLinesAsync(file, lines);
                    fileCount++;
                }

            await new Windows.UI.Popups.MessageDialog(fileCount + " file(s) written.").ShowAsync();
        }

        private async void SessionsTrialsEstimationsToCsvButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            //var trials = new List<Trial>();

            //var trials = new List<Trial>();
            //foreach (List<Trial> tl in sessions.Select(s => s.Trials))
            //    trials.AddRange(tl);

            await WriteTrialsEstimations(SelectedSessions);

            //foreach(CheckBox cb in Sessions2ItemsControl.Sele) 
        }

        private void CheckBox_Tapped(object sender, RoutedEventArgs e)
        {
        }

        private void CheckBox_Tapped_1(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            //var cb = (sender as CheckBox);

            //string name = (string)(sender as CheckBox).Content;
            //Trial trial = SelectedSession.Trials.Find(t => t.Name.Equals(name));
        }

        private void SessionsItemsControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            //Trial trial = (Trial)(sender as CheckBox).Tag;
            TrialTextBlock.Text = StringExpert.ToJson((sender as ListBox).SelectedItem);

        }

        private string PropertyToCsvColName(PropertyInfo p)
        {
            return propertyStringMappings[p];
        }

        private PropertyInfo CsvColNameToPropery(string n)
        {
            foreach (var key in propertyStringMappings.Keys)
                if (propertyStringMappings[key] == n)
                    return key;

            throw new Exception("Value not found");
        }

        private async void WriteDevciceInfoButton_Click(object sender, RoutedEventArgs e)
        {
            var off = AppSettings.InputSpace.Offscreen;
            var on = AppSettings.InputSpace.Onscreen;
            var data = new Dictionary<string, object> {
                { "onscreenPixelHeight", on.Height},
                { "onscreenPixelWidth", on.Width},
            };

            await WriteCsvDataFile(
                new List<string> {
                    StringExpert.JoinByTab(data.Keys),
                    StringExpert.JoinByTab(data.Values),
                },
                "device.csv"
                );

            new Windows.UI.Popups.MessageDialog(string.Format(
                "Device info written"
                )).ShowAsync();


        }

        static readonly int[] angles = new int[] { 0, 30, 60, 90, 120, 150, 180, 210, 240, 270, 300, 330 };

        const double TARGET_VALUE_TO_SAVE_IN_SETTINGS = 50;

        private static IEnumerable<string> toLine(double targetValue, IEnumerable<Trial> trials, string PCA_PATTERN, bool isToMapWithSkew = true)
        {

            List<XYPoint> releasePositions =
                trials.Select(
                t => new XYPoint(
                    t.ReleasePositionX,
                    t.ReleasePositionY
                    )
                //sg => new XYPoint(
                //    sg.PropertyStats[pcaPropX].mean,
                //    sg.PropertyStats[pcaPropY].mean
                //    )
                ).ToList();

            var pointRemapper = new Exp3PointMapper(releasePositions);

            if (targetValue == TARGET_VALUE_TO_SAVE_IN_SETTINGS)
                AppSettings.Exp3PointRemapper = pointRemapper; // (somewhat hacky)

            //double xr = pca.RadiusX; //Math.Sqrt(pca.Eig1VectorUnit.Eigenvalue);
            //double yr = pca.RadiusY; //Math.Sqrt(pca.Eig2VectorUnit.Eigenvalue);

            //double circleRadiusPixels = MathExpert.GetCircleRadius(MathExpert.EllipsePerimeteRamanujanrApprox(xr, yr));

            var patternParams = new List<object>
                {
                        pointRemapper.PCA.Mean.X,
                        pointRemapper.PCA.Mean.Y,
                        pointRemapper.PCA.PrincipalAngle,
                        pointRemapper.PCA.PrincipalAngleDegree,
                        pointRemapper.RadiusX, pointRemapper.RadiusY,
                        //xStepR, yStepR,
                        targetValue,
                        pointRemapper.CircleRadius,// circleRadiusPixels,
                        ((int)(AppSettings.OnscreenDistanceToMillimeter(pointRemapper.CircleRadius) /10.0))/100.0, // to make it to meters wit two digits
                    };

            //for (int ringNumber = 1; ringNumber <= 5; ringNumber++)
            foreach (var angle in angles)
            {
                var rad = GeometryExpert.DegreeToRadian(angle);

                //var radiusPixels = TestPage.TICK_DISTANCE_PIXELS * 50;
                var radiusPixels = TestPage.TICK_DISTANCE_PIXELS * targetValue;

                PlanePoint tp = new XYPoint(Math.Cos(rad) * radiusPixels, Math.Sin(rad) * -radiusPixels);
                tp = pointRemapper.FromNormalToModeledSpace(tp, isToMapWithSkew);
                //tp = tp.Multiply(4 / 5.0);

                patternParams.Add(tp.X);
                patternParams.Add(tp.Y);
            }

            yield return string.Format(PCA_PATTERN, patternParams.ToArray());
        }


        private static IEnumerable<string> toLines(double targetValue, IEnumerable<Trial> trials, bool isToMapWithSkew = true)
        {
            //const double TARGET_VALUE_TO_SAVE_IN_SETTINGS = 50;

            List<XYPoint> releasePositions =
                trials.Select(
                t => new XYPoint(
                    t.ReleasePositionX,
                    t.ReleasePositionY
                    )
                //sg => new XYPoint(
                //    sg.PropertyStats[pcaPropX].mean,
                //    sg.PropertyStats[pcaPropY].mean
                //    )
                ).ToList();

            var pointRemapper = new Exp3PointMapper(releasePositions);

            //if (targetValue == TARGET_VALUE_TO_SAVE_IN_SETTINGS)
            //    AppSettings.Exp3PointRemapper = pointRemapper;

            //double xr = pca.RadiusX; //Math.Sqrt(pca.Eig1VectorUnit.Eigenvalue);
            //double yr = pca.RadiusY; //Math.Sqrt(pca.Eig2VectorUnit.Eigenvalue);

            //double circleRadiusPixels = MathExpert.GetCircleRadius(MathExpert.EllipsePerimeteRamanujanrApprox(xr, yr));

            //var patternParams = new List<object>
            //    {
            //            pointRemapper.PCA.Mean.X,
            //            pointRemapper.PCA.Mean.Y,
            //            pointRemapper.PCA.PrincipalAngle,
            //            pointRemapper.PCA.PrincipalAngleDegree,
            //            pointRemapper.RadiusX, pointRemapper.RadiusY,
            //            //xStepR, yStepR,
            //            targetValue,
            //            pointRemapper.CircleRadius,// circleRadiusPixels,
            //            ((int)(AppSettings.OnscreenDistanceToMillimeter(pointRemapper.CircleRadius) /10.0))/100.0, // to make it to meters wit two digits
            //        };

            List<int> fullCircle = Enumerable.Range(1, 360).Select((v, i) => i + 1).ToList();
            List<int> radialAngles = Enumerable.Range(1, 360 / 30).Select((a, i) => i * 30).ToList();

            for (int t = 3; t <= 50; t++)  // = HOLD-OFF
            {
                bool isTargetCircle = (t % 10 == 0);
                List<int> degs = isTargetCircle ? fullCircle : radialAngles;

                foreach (var deg in degs)
                //for (int deg = 0; deg <3 60; deg++)
                //foreach (var angle in angles)
                {

                    var rad = GeometryExpert.DegreeToRadian(deg);



                    var radiusPixels = TestPage.TICK_DISTANCE_PIXELS * t; //* 10 * ringNumber;

                    PlanePoint tp = new XYPoint(Math.Cos(rad) * radiusPixels, Math.Sin(rad) * -radiusPixels);

                    tp = pointRemapper.FromNormalToModeledSpace(tp, isToMapWithSkew);

                    //tp = pointRemapper.FromModeledToNormalSpace(tp)

                    //tp = tp.Multiply(4 / 5.0);

                    //patternParams.Add(tp.X);
                    //patternParams.Add(tp.Y);

                    //if (isTargetCircle)
                    //    yield return StringExpert.JoinManyByTab(
                    //        "ellipse",
                    //        t,
                    //        rad, deg,
                    //        tp.X, tp.Y,
                    //        1
                    //        );

                    bool isTargetRadial = (radialAngles.Contains(deg));

                    yield return StringExpert.JoinManyByTab(
                        "radial",
                        t,
                        rad, deg,
                        tp.X, tp.Y,
                        isTargetCircle ? "1" : "0",
                        isTargetRadial ? "1" : "0"
                        );
                }
            }
        }

        private async void Exp3MapDeriveButton_Click(object sender, RoutedEventArgs e)
        {
            //var mapper = AppSettings.Exp3PointRemapper;
            //mapper.DeriveTransformMap();
            //AppSettings.Exp3PointRemapper = mapper;
            throw new NotImplementedException();
        }

        private async void PCAButton_Click(object sender, RoutedEventArgs e)
        {

            List<string> PCA_COLS = new List<string> {
                "x", "y", "phi", "deg",
                //"uxr", "uyr",
                "xr", "yr",
                //"xStepR", "yStepR",
                "target",
                "circleRadius",
                "circleRadiusM",
                //"tpx", "tpy"
                };

            PCA_COLS = PCA_COLS.Concat(angles.SelectMany(a =>
                new string[] {
                    "angle" + a + "x",
                    "angle" + a + "y",
                }
                )).ToList();

            string PCA_PATTERN = StringExpert.Join(
                PCA_COLS.Select((l, i) => "{" + i + "}"),
                "\t");


            PropertyInfo pcaPropX = GetProps<Trial, CsvPcaXAttribute>().Single();
            PropertyInfo pcaPropY = GetProps<Trial, CsvPcaYAttribute>().Single();

            var completedE2Sessions = GetCompletedSessions().Where(
                s => s.Type == TrialSessionType.E2
                ).ToList();

            var getSessionByTrial = new Func<Trial, TrialSession>(t => completedE2Sessions.First(s => s.Trials.Contains(t)));

            List<StatsGroup> allE2StatsGroups = GetGroupedTrialsWithStats(completedE2Sessions, getSessionByTrial);

            /////////// PCA OUTLIERS
            foreach (var s in allE2StatsGroups.GroupBy(g => Math.Abs(g.TargetValue)))
            {
                var target = s.Key;

                OutlierFilter f = new OutlierFilter(
                    s.SelectMany(g => g.Select(t => t.FromTargetToReleaseLength))
                    );

                var outliers = s.SelectMany(g => g).Where(t => f.IsExtremeOutlier(t.FromTargetToReleaseLength)).ToList();
                var nonoutliers = s.SelectMany(g => g).Where(t => !f.IsExtremeOutlier(t.FromTargetToReleaseLength)).ToList();

                var mean = nonoutliers.Select(t => t.FromTargetToReleasePosition).Aggregate((p1, p2) => new Point(
                    (p1.X + p2.X) / nonoutliers.Count,
                    (p1.Y + p2.Y) / nonoutliers.Count
                    ));
                foreach (var outlier in outliers)
                    outlier.FromTargetToReleasePosition = mean;
            }
            ////////////

            var files = new Dictionary<string, IEnumerable<string>>
            {
                { "PCA-aggregated.csv",
                    new int[] { 10, 20, 30, 40, 50 }.Select(absTargetValue => toLine(
                        absTargetValue,
                        allE2StatsGroups.Where(
                                sg => Math.Abs(sg.TargetValue) <= absTargetValue)
                                .SelectMany(sg => sg) // concat to trials
                        ,PCA_PATTERN)
                    ).SelectMany(s=>s)
                },
                { "PCA-targetValue(50).csv",
                    new int[] { 50 }.Select(absTargetValue => toLine(
                        absTargetValue,
                        allE2StatsGroups.Where(
                                sg => Math.Abs(sg.TargetValue) == absTargetValue)
                                //.Where(sg => sg.TargetValue == 10 || sg.TargetValue == -10)
                                .SelectMany(sg => sg) // concat to trials
                        ,PCA_PATTERN)
                    ).SelectMany(s=>s)
                  //allStatsGroups.Where(sg => Math.Abs(sg.TargetValue) == 50).Select(sg => 
                  //  toLine(50,sg)
                  //)
                },
                { "PCA.csv",
                  allE2StatsGroups.GroupBy(sg => Math.Abs(sg.TargetValue)).Select(sg => toLine(
                        sg.Key,
                        sg.SelectMany(sgOfTarget => sgOfTarget)
                        ,PCA_PATTERN
                    )).SelectMany(s=>s)
                },
                { "PCA-noskew.csv",
                  allE2StatsGroups.GroupBy(sg => Math.Abs(sg.TargetValue)).Select(sg => toLine(
                        sg.Key,
                        sg.SelectMany(sgOfTarget => sgOfTarget)
                        ,PCA_PATTERN,
                        false
                    )).SelectMany(s=>s)
                },
            };

            foreach (string filename in files.Keys)
            {
                var lines = new List<string> {
                    string.Format(PCA_PATTERN, PCA_COLS.ToArray())
                };
                lines.AddRange(files[filename]);
                await WriteCsvDataFile(lines, filename);
            }

            ///////////SKEW DATA////////
            await WriteSkewFile(allE2StatsGroups, "skewedCircles.csv", true);
            await WriteSkewFile(allE2StatsGroups, "nonskewedCircles.csv", false);

            /////////////////////


            //// add circle line for each (absolute) target value
            //List<string> lines = new List<string> { string.Format(PCA_PATTERN, PCA_COLS.ToArray()) };
            //foreach (var group in allStatsGroups.GroupBy(sg => Math.Abs(sg.TargetValue)))
            //{
            //    var targetValue = group.Key;

            //    List<XYPoint> releasePositions =
            //        group
            //        .SelectMany(sg => sg)
            //        .Select(
            //        t => new XYPoint(
            //            t.ReleasePositionX,
            //            t.ReleasePositionY
            //            )
            //        //sg => new XYPoint(
            //        //    sg.PropertyStats[pcaPropX].mean,
            //        //    sg.PropertyStats[pcaPropY].mean
            //        //    )
            //        ).ToList();

            //    var pca = new MyPCA(releasePositions);

            //    lines.Add(string.Format(PCA_PATTERN,
            //        pca.Mean.X,
            //        pca.Mean.Y,
            //        pca.PrincipalAngle,
            //        pca.PrincipalAngleDegree,
            //        Math.Sqrt(pca.Eig1VectorUnit.Eigenvalue),
            //        Math.Sqrt(pca.Eig2VectorUnit.Eigenvalue),
            //        targetValue
            //        ));
            //}

            //await WriteCsvDataFile(lines, "PCA.csv");


            //await WriteNonaggreatedPCAS(allStatsGroups);

            //await WriteAggreatedPCAs(allStatsGroups);

            new Windows.UI.Popups.MessageDialog(string.Format(
                "PCA data written"
                )).ShowAsync();

        }

        private async Task WriteSkewFile(List<StatsGroup> allStatsGroups, string filename, bool isToMapWithSkew)
        {
            var groupsOfAbsTarget50 = allStatsGroups.Where(
                                sg => Math.Abs(sg.TargetValue) == 50)
                                //.Where(sg => sg.TargetValue == 10 || sg.TargetValue == -10)
                                .SelectMany(sg => sg); // concat to trials

            await WriteCsvDataFile(
                new List<string> { StringExpert.JoinManyByTab("type", "target", "rad", "deg", "x", "y", "isEllipse", "isRadial") }.Concat(
                toLines(
                    50,
                    groupsOfAbsTarget50, isToMapWithSkew)
                ),
                filename
                );
        }

        //private async Task WriteAggreatedPCAs(List<StatsGroup> allStatsGroups)
        //{
        //    List<string> lines = new List<string> { string.Format(PCA_PATTERN, PCA_COLS.ToArray()) };
        //    foreach (var targetValue in new int[] { 10, 20, 30, 40, 50 })
        //    {
        //        List<XYPoint> releasePositions =
        //            allStatsGroups.Where(sg => Math.Abs(sg.TargetValue) <= targetValue)
        //            //.Where(sg => sg.TargetValue == 10 || sg.TargetValue == -10)
        //            .SelectMany(sg => sg)
        //            .Select(
        //            t => new XYPoint(
        //                t.ReleasePositionX,
        //                t.ReleasePositionY
        //                )
        //            //.Select(
        //            //sg => new XYPoint(
        //            //    sg.PropertyStats[pcaPropX].mean,
        //            //    sg.PropertyStats[pcaPropY].mean
        //            //    )
        //            ).ToList();

        //        var pca = new MyPCA(releasePositions);

        //        lines.Add(string.Format(PCA_PATTERN,
        //            pca.Mean.X,
        //            pca.Mean.Y,
        //            pca.PrincipalAngle,
        //            pca.PrincipalAngleDegree,
        //            Math.Sqrt(pca.Eig1VectorUnit.Eigenvalue),
        //            Math.Sqrt(pca.Eig2VectorUnit.Eigenvalue),
        //            targetValue
        //            ));
        //    }

        //    await WriteCsvDataFile(lines, "PCA-aggregated.csv");
        //}

        //private async Task WriteNonaggreatedPCAS(List<StatsGroup> allStatsGroups)
        //{

        //    // add circle line for each (absolute) target value
        //    List<string> lines = new List<string> { string.Format(PCA_PATTERN, PCA_COLS.ToArray()) };
        //    foreach (var group in allStatsGroups.GroupBy(sg => Math.Abs(sg.TargetValue)))
        //    {
        //        var targetValue = group.Key;

        //        List<XYPoint> releasePositions =
        //            group
        //            .SelectMany(sg => sg)
        //            .Select(
        //            t => new XYPoint(
        //                t.ReleasePositionX,
        //                t.ReleasePositionY
        //                )
        //            //sg => new XYPoint(
        //            //    sg.PropertyStats[pcaPropX].mean,
        //            //    sg.PropertyStats[pcaPropY].mean
        //            //    )
        //            ).ToList();

        //        var pca = new MyPCA(releasePositions);

        //        lines.Add(string.Format(PCA_PATTERN,
        //            pca.Mean.X,
        //            pca.Mean.Y,
        //            pca.PrincipalAngle,
        //            pca.PrincipalAngleDegree,
        //            Math.Sqrt(pca.Eig1VectorUnit.Eigenvalue),
        //            Math.Sqrt(pca.Eig2VectorUnit.Eigenvalue),
        //            targetValue
        //            ));
        //    }

        //    await WriteCsvDataFile(lines, "PCA.csv");
        //}

        private static double getTrialDoubleValue(Trial t, PropertyInfo p)
        {
            return (p.PropertyType == typeof(int)) ?
                (double)(int)p.GetValue(t) :
                (double)p.GetValue(t);
        }

        private void AllCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            List<TrialSession> sessions = GetCompletedSessions();

            SetTrialsExp3Target(sessions);


            if (sessions.Count() == 0)
                return;

            ToCsv(sessions);
        }

        private List<TrialSession> GetCompletedSessions()
        {
            return Sessions2ItemsControl.Items.Cast<SelectableSession>().Where(i =>
                i.Session.IsSessionCompleted &&
                (i.Session.Type == TrialSessionType.E1 || i.Session.Type == TrialSessionType.E2 || i.Session.Type == TrialSessionType.E3)
                ).Select(i => i.Session).ToList();
        }

        private void FullCSVButton_Click(object sender, RoutedEventArgs e)
        {
            var sessions = new List<TrialSession>(SelectedSessions);
            if (sessions.Count() == 0)
                return;

            ToCsv(sessions);
        }

        private async void ToCsv(List<TrialSession> sessions)
        {
            if (sessions.Count(s => !s.IsSessionCompleted) > 0)
            {
                if (OnlyCompletedTrialsCheckBox.IsChecked.Value)
                    throw new Exception("One or more sessions have not been completed.");
            }

            int fileCount = 0;






            //var trials = new List<Trial>();

            //int outlierDetectionIterationsCompleted = 0;
            //int outlierDetectionIterationsSkipped = 0;


            var getSessionByTrial = new Func<Trial, TrialSession>(t => sessions.First(s => s.Trials.Contains(t)));

            List<StatsGroup> allStatsGroups = GetGroupedTrialsWithStats(sessions, getSessionByTrial);

            var outlierTrials = allStatsGroups.SelectMany(g => g.Where(t => t.IsOutlier));
            log.Info("Number of trials with one or more outlier props: " + outlierTrials.Count() + " (" + StringExpert.CommaSeparate(outlierTrials.Select(t => t.Name)) + ")");

            string sessionNameStringToken = StringExpert.CommaSeparate(sessions.Select(s => s.Name).Distinct());


            var e1statGroups = allStatsGroups.Where(g => g.SessionType == TrialSessionType.E1).ToList();
            var e2statGroups = allStatsGroups.Where(g => g.SessionType == TrialSessionType.E2).ToList();
            var e3statGroups = allStatsGroups.Where(g => g.SessionType == TrialSessionType.E3).ToList();

            const string TARGET_SESSION_NAME = "target";
            //const string EXP3_TARGET_SESSION_NAME = "exp3target";

            //xColIndex =  trialFromTargetToReleasePositionX
            //"trialFromTargetToReleasePositionX"
            var e2statGroupsPolarTable = StatGroupsToTableWithStatsRows(getSessionByTrial, e2statGroups)
                //.TrimByRowsColValue(
                //"sessionName", MEAN_SESSION_NAME
                //)
                .DuplicateRows(
                    colsFilter: new Dictionary<string, Func<object, bool>>() { // only duplicate the mean columns 
                        { "sessionName", sessionName => sessionName.Equals(MEAN_SESSION_NAME) },
                    },
                    colValueManipulate: new Dictionary<string, Func<object, object>>() {  // name the duplicates 'target'
                        { "sessionName", sessionName => TARGET_SESSION_NAME },
                    },
                    colNameValueMapping: new Dictionary<string, string>() { // have the duplicate 'target' cols have release values corresponding to the true position of the target
                        { "trialReleasePositionX", "trialFromOrigoToTargetPositionX" },
                        { "trialReleasePositionY", "trialFromOrigoToTargetPositionY" },
                    }
                    );


            var e3statGroupsPolarTable = StatGroupsToTableWithStatsRows(getSessionByTrial, e3statGroups)
                //.TrimByRowsColValue(
                //"sessionName", MEAN_SESSION_NAME
                //)
                .DuplicateRows(
                    colsFilter: new Dictionary<string, Func<object, bool>>() { // only duplicate the mean columns 
                        { "sessionName", sessionName => sessionName.Equals(MEAN_SESSION_NAME) },
                    },
                    colValueManipulate: new Dictionary<string, Func<object, object>>() {  // name the duplicates 'target'
                        { "sessionName", sessionName => TARGET_SESSION_NAME },
                    },
                    colNameValueMapping: new Dictionary<string, string>() { // have the duplicate 'target' cols have release values corresponding to the true position of the target
                        { "trialReleasePositionX", "trialFromOrigoToTargetPositionX" },
                        { "trialReleasePositionY", "trialFromOrigoToTargetPositionY" },
                    }
                    );



            //var e2PolarSpecialCase = e2statsGroups.Where(g => g.)

            // split data to make
            var divisions = new Dictionary<string, MyDataTable> {
                {
                    sessionNameStringToken ,
                    StatGroupsToTableWithStatsRows(getSessionByTrial, allStatsGroups)
                },
                {
                    "E1-" + sessionNameStringToken,
                    StatGroupsToTableWithStatsRows(getSessionByTrial, e1statGroups)
                },
                {
                    "E2-" + sessionNameStringToken,
                    StatGroupsToTableWithStatsRows(getSessionByTrial, e2statGroups)
                },
                {
                    "E2-mean,target," + sessionNameStringToken,
                    e2statGroupsPolarTable
                },
                {
                    "E2-mean,target",
                    e2statGroupsPolarTable.Filter(new Dictionary<string, List<string>> { {
                        "sessionName", new List<string> { MEAN_SESSION_NAME, TARGET_SESSION_NAME } }
                    })
                },
                {
                    "E3-mean,target," + sessionNameStringToken,
                    e3statGroupsPolarTable
                },
            };

            for (int target = -50; target <= 50; target += (target == -10 ? 20 : 10))
            {
                for (int angle = 0; angle <= 150; angle += 30)
                    divisions[string.Format("E2-mean,target-trialTargetValue({0}),trialUIAngleDegrees({1})", target, angle)] = e2statGroupsPolarTable.Filter(new Dictionary<string, List<string>>  {
                    { "sessionName", new List<string> { MEAN_SESSION_NAME, TARGET_SESSION_NAME } },
                    { "trialTargetValue", new List<string> { target.ToString() } },
                    { "trialUIAngleDegrees", new List<string> { angle.ToString() } }
                    });
            }

            //allStatsGroups.SelectMany(g => g.Select(t=> getSessionByTrial(t).Name)).Distinct(). ToList();
            //for each session
            var sessionNames = sessions.Select(s => s.Name).Distinct().ToList(); //allStatsGroups.SelectMany(g => g.Select(t=> getSessionByTrial(t).Name)).Distinct(). ToList();
            foreach (string sessionName in sessionNames)
            {
                divisions["E2-" + sessionName] = StatGroupsToTableWithStatsRows(getSessionByTrial, e2statGroups).Filter(new Dictionary<string, List<string>> {
                        { "sessionName", new List<string> { sessionName } },
                        //{ "trialFromTargetToReleaseLength" + OUTLIER_POSTFIX, new List<object> { "0" } },
                    });

                for (int target = -50; target <= 50; target += (target == -10 ? 20 : 10))
                    divisions["E2-" + sessionName + ",target(" + target + ")"] = StatGroupsToTableWithStatsRows(getSessionByTrial, e2statGroups).Filter(new Dictionary<string, List<string>> {
                        { "sessionName", new List<string> { sessionName } },
                        { "trialTargetValue", new List<string> { target.ToString() } },
                        //{ "trialFromTargetToReleaseLength" + OUTLIER_POSTFIX, new List<object> { "0" } },
                    });


                //divisions["E2-outlier-" + sessionName] = StatGroupsToTableWithStatsRows(getSessionByTrial, e2statGroups).Filter(new Dictionary<string, List<object>> {
                //        { "sessionName", new List<object> { sessionName } },
                //        { "trialFromTargetToReleaseLength" + OUTLIER_POSTFIX, new List<object> { "1" } },
                //    });
            }


            //List<string> sessionNames = statsGroups.Select(g => g.)

            //var exp2PolarPlotGroups =  

            foreach (string infix in divisions.Keys)
            {
                //await WriteStatsGroupsToFile(
                //    infix,
                //    getSessionByTrial,
                //    divisions[infix].ToList()
                //    );

                //MyDataTable table = StatGroupsToTable(getSessionByTrial, divisions[infix].ToList());


                await WriteTableToFile(
                    infix,
                    divisions[infix]
                    );
                fileCount++;
            }

            await new Windows.UI.Popups.MessageDialog(
                //string.Format("{0}/{1} trials written to file ({2} outliers removed using Q1={3} and Q4={4} giving range={5}).", trials.Count, trials.Count - outlierCount, outlierCount, durationSecondsLowerQuartile, durationSecondsUpperQuartile, durationSecondsUpperQuartile - durationSecondsLowerQuartile)
                "Finished " + fileCount + " file write attempts."
                ).ShowAsync();

            //await WriteStatsGroupsToFile(
            //    sessions.Select(s => s.Name).Aggregate((n1, n2) => n1 + "," + n2),
            //    getSessionByTrial,
            //    statsGroups
            //    );
        }

        private List<StatsGroup> GetGroupedTrialsWithStats(List<TrialSession> sessions, Func<Trial, TrialSession> getSessionByTrial)
        {
            SetTrialsExp3Target(sessions);

            SetTrialsFinalOffscreenLocationRelativeToInputRecit(sessions);

            DuplicateDerivedTrials(sessions);

            HandleDimsConcatCollapseTrialsSpecialCase(sessions);

            List<StatsGroup> allStatsGroups = new List<StatsGroup>();




            //foreach (var sessionsOfType in sessions.GroupBy(s => s.Type))
            foreach (var sessionsOfType in sessions.GroupBy(s => s.Type))
            {
                TrialSessionType sessionType = sessionsOfType.Key;
                log.Verbose("--- Processing sessions of type: " + sessionType + " ---");

                //foreach (var sessionOfType in sessionsOfType) // for each experiment type
                //{
                //var nonEstimationTrials = sessionOfType.Trials.Where(t => !t.MoveBullsEyeInsteadOfMap);

                var sessionTypeTrials = sessionsOfType.SelectMany(s => s.Trials);

                foreach (var trialsInGroup in sessionTypeTrials.GroupBy(t => t.GroupName)) //  for each projection mode
                {
                    string groupName = trialsInGroup.Key;
                    log.Verbose("\t** Processing group name: " + groupName);


                    foreach (var trialsOfProjectionMode in trialsInGroup.GroupBy(t => t.ProjectionMode)) //  for each projection mode
                    {
                        ProjectionMode projectionMode = trialsOfProjectionMode.Key;
                        //log.Verbose("\t\t** Processing projection mode: " + projectionMode);

                        foreach (var trialsOfProjectionModeAndDimsMode in trialsOfProjectionMode.OrderBy(t => t.TargetValue).GroupBy(t => t.Mode)) //  for each dimensional mode
                        {
                            TrialMode trialModel = trialsOfProjectionModeAndDimsMode.Key;

                            //log.Verbose("\t\t\t** Processing trial dims mode: " + trialModel);

                            foreach (var trialsOfProjectionModeDimsModeAndEstimationOrNot in trialsOfProjectionModeAndDimsMode.GroupBy(t => t.MoveBullsEyeInsteadOfMap)) // for both estimation and non-estimation mode
                            {
                                bool isEstimationMode = trialsOfProjectionModeDimsModeAndEstimationOrNot.Key;

                                foreach (var exp3SpaceUsage in trialsOfProjectionModeDimsModeAndEstimationOrNot.GroupBy(t => t.IsExp3SpaceToBeUsed))
                                {
                                    bool isExp3SpaceToBeUsed = exp3SpaceUsage.Key;

                                    foreach (var trialsOfProjectionModeDimsModEstimationOrNotAndValue in exp3SpaceUsage.GroupBy(t => t.TargetValue)) //  for each target value, do outlier detectec
                                    {
                                        //}//  for each target value, do outlier detectec
                                        double target = trialsOfProjectionModeDimsModEstimationOrNotAndValue.Key;

                                        //log.Verbose("\t\t\t\t** Processing target value: " + target);




                                        foreach (var angledTrials in trialsOfProjectionModeDimsModEstimationOrNotAndValue.GroupBy(t => t.UIAngle))
                                        {
                                            double angle = angledTrials.Key;
                                            //log.Verbose("\t\t\t\t\t** Processing angle: " + angle + " (" + GeometryExpert.RadianToDegree(angle) + ")");

                                            var completedStatsGroupTrials = angledTrials.Where(t => t.IsCompleted).ToList();

                                            //List<Trial> nonOutlierTrials = statsGroupTrials.ToList();

                                            foreach (Trial trial in completedStatsGroupTrials)
                                            {
                                                //table.AddRow();



                                                foreach (PropertyInfo p in TrialStatsProps) //(string csvColName in statsColsMap.Keys)
                                                {
                                                    //string csvColName = propertyStringMappings[p];
                                                    //string csvPropOutlierIndicatorColName = csvColName + "IsOutlier";
                                                    //table.CreateColIfNotExists(csvPropOutlierIndicatorColName, 0);

                                                    //{
                                                    //outlierDetectionIterationsSkipped++;
                                                    //log.Error("\t\t\tToo few trials to do outlier detection");
                                                    //outlierDetectionIterationsCompleted++;

                                                    //var allDuration%Seconds = statsGroupTrials.Select(t => t.DurationSeconds); //sessions.SelectMany(s => s.Trials).Select(t => t.DurationSeconds);

                                                    //); //sessions.SelectMany(s => s.Trials).Select(t => t.DurationSeconds));

                                                    //var allTrialsPropValues = ;

                                                    //double lowerQuartile = MathExpert.GetLowerQuartile(allTrialsPropValues);
                                                    //double upperQuartile = MathExpert.GetUpperQuartile(allTrialsPropValues);

                                                    //double interquartileRange = upperQuartile - lowerQuartile;

                                                    //double fenceMin = lowerQuartile - 3 * interquartileRange;
                                                    //double fenceMax = upperQuartile + 3 * interquartileRange;

                                                    // TODO CHECK IF OUTLIER CASE
                                                    var s = getSessionByTrial(trial);
                                                    if (s.Name == "leah" && trial.TargetValue == -50 & trial.Mode == TrialMode.Vertical && trial.ProjectionMode == ProjectionMode.Spherical)
                                                    { }


                                                    double trialPropValue = getTrialDoubleValue(trial, p);

                                                    if (getSessionByTrial(trial).Type == TrialSessionType.E3)
                                                    { }

                                                    // outliers: first check for hardcoded outlier limit..
                                                    if (PropHasAttribute<OutlierLimitAttribute>(p))
                                                    {

                                                        double outlierLimit = GetAttribute<OutlierLimitAttribute>(p).Limit;
                                                        bool isValueDefinedAsOutlier = (trialPropValue >= outlierLimit);

                                                        trial.IsOutlier = isValueDefinedAsOutlier;

                                                        if (isValueDefinedAsOutlier)
                                                        {
                                                            log.Verbose("\t\t\t\t\t   !!!HARD LIMIT OUTLIER DETECTED: " + p.Name + ":" + trialPropValue) ;
                                                            //log.Verbose("\t\t\t\t\t\t\t {4}:  {0} <= {1} <= {2} = {3}", trialPropValue, outlierFilter.upperOuterFrence, !isTrialPropExtremeOutlier ? "YES" : "NO", (isTrialPropExtremeOutlier ? "OUTLIER! " : "") + p.Name);
                                                        }
                                                    }


                                                    bool canDoOutlierDetection = (completedStatsGroupTrials.Count() >= 4);
                                                    bool isTrialPropExtremeOutlier = false;
                                                    if (!trial.IsOutlier)
                                                        if (canDoOutlierDetection)
                                                        {
                                                            // outliers: ..then check using IQ 
                                                            var outlierFilterValues = completedStatsGroupTrials.Select(t => getTrialDoubleValue(t, p)).ToList();
                                                            var outlierFilter = new OutlierFilter(outlierFilterValues);

                                                            isTrialPropExtremeOutlier = outlierFilter.IsExtremeOutlier(trialPropValue);
                                                            bool isTrialPropMildOutlier = outlierFilter.IsMildOutlier(trialPropValue);

                                                            trial.IsOutlier = isTrialPropExtremeOutlier;

                                                            if (isTrialPropExtremeOutlier)
                                                            {
                                                                log.Verbose("\t\t\t\t\t\t TRIAL OUTLIER DETECTED!");

                                                                //log.Verbose("\t\t\t\t\t\t ------");
                                                                log.Verbose("\t\t\t\t\t\t\t {4}:  {0} <= {1} <= {2} = {3}", outlierFilter.lowerOuterFence, trialPropValue, outlierFilter.upperOuterFrence, !isTrialPropExtremeOutlier ? "YES" : "NO", (isTrialPropExtremeOutlier ? "OUTLIER! " : "") + p.Name);
                                                            }
                                                        }
                                                    //table.SetCurrentRowCol(csvPropOutlierIndicatorColName, isTrialPropOutlier);
                                                    if (trial.IsOutlier)
                                                    {
                                                        //if (!trial.IsOutlier)


                                                        if (!trial.OutlierProps.Contains(p))
                                                            trial.OutlierProps.Add(p);
                                                    }
                                                    //log.Verbose("\t\t\t\t\t\t ------");
                                                    //log.Verbose("\t\t\t\t\t\t\t {4}:  {0} <= {1} <= {2} = {3}", fenceMin, trialPropValue, fenceMax, !isTrialPropOutlier ? "YES" : "NO", (isTrialPropOutlier ? "OUTLIER! " : "") + p.Name);
                                                    //}
                                                }


                                                //WriteSesssionAndTrialCsvPropertiesToCurrentTableRow(table, getSessionByTrial(trial), trial);

                                                if (trial.IsOutlier)
                                                {
                                                    log.Info("\t\t\t\t\t** Marked outlier props for (session name: {0}, session type: {1}, projection mode: {2}, dims mode: {3}, target: {4}, angle:{6}): {5}",
                                                        getSessionByTrial(trial).Name,
                                                        sessionType,
                                                        projectionMode,
                                                        trialModel,
                                                        target,
                                                        StringExpert.CommaSeparate(trial.OutlierProps.Select(p => string.Format("({0}:{1})", p.Name, getTrialDoubleValue(trial, p)))),
                                                        angle
                                                        );

                                                    if (getSessionByTrial(trial).Type == TrialSessionType.E1) { }
                                                }
                                            }



                                            var statsGroup = new StatsGroup(completedStatsGroupTrials)
                                            {
                                                UIAngle = angle,
                                                GroupName = groupName,
                                                ProjectionMode = projectionMode,
                                                TrialMode = trialModel,
                                                IsEstimationMode = isEstimationMode,
                                                TargetValue = target,
                                                SessionType = sessionType,
                                                TargetLocation = completedStatsGroupTrials.First().FromOrigoToTargetPosition, // shuold be the3 same for all in target value and angle combi
                                                Exp3ReleasePosition = completedStatsGroupTrials.First().FromOrigoToExp3ReleasePosition,
                                                IsExp3SpaceToBeUsed = isExp3SpaceToBeUsed
                                                //Exp3TargetPosition = completedStatsGroupTrials.First().FromOrigoToExp3TargetPosition // shuold be the3 same for all in target value and angle combi
                                            };
                                            allStatsGroups.Add(statsGroup);

                                            //IEnumerable<Trial> trialsWithNameAndTargetValue = sessionTypeTrialsWithName.Where(t => t.TargetValue == targetValue);
                                            //int trialCount = nonOutlierTrials.Count();



                                            // add stats rows (mean, std, ..) for each property to do stats on 

                                            //var nonOutlierTrials = completedStatsGroupTrials.Where(t => !t.IsOutlier).ToList();

                                            var getCompletedNonoutlierPropValues = new Func<PropertyInfo, IEnumerable<double>>(
                                                (p) => completedStatsGroupTrials.Where(
                                                    t => !t.OutlierProps.Contains(p)
                                                    ).Select(
                                                        t => getTrialDoubleValue(t, p)
                                                        )
                                                );


                                            //AddInitializedStatsRow("mean");
                                            foreach (PropertyInfo p in TrialStatsProps) //(string csvColName in statsColsMap.Keys)
                                            {
                                                IEnumerable<double> colValues = getCompletedNonoutlierPropValues(p);   // trialsWithNameAndTargetValue.Select(t => GetTrialValue(csvColName, t));

                                                //string csvColName = propertyStringMappings[p];
                                                //table.SetCurrentRowCol(csvColName, MathExpert.GetMean(colValues));

                                                //string csvConfidenceColName = csvColName + "Confidence";
                                                //table.CreateColIfNotExists(csvConfidenceColName);
                                                ////if (targetValue==15)
                                                ////{ }
                                                //table.SetCurrentRowCol(csvConfidenceColName, MathExpert.GetNinetyFiveConfMarginOfError(colValues));

                                                statsGroup.PropertyStats[p] = new PropertyStat
                                                {
                                                    mean = MathExpert.GetMean(colValues),
                                                    confidence = MathExpert.GetNinetyFiveConfMarginOfError(colValues),
                                                    std = MathExpert.GetStandardDeviation(colValues)
                                                };

                                                //statsGroup.Means[p] = MathExpert.GetMean(colValues);
                                                //statsGroup.Confidences[p] = MathExpert.GetNinetyFiveConfMarginOfError(colValues);

                                                //statsGroup.Std[p] = MathExpert.GetStandardDeviation(colValues);
                                            }

                                            ////AddInitializedStatsRow("std");
                                            //foreach (PropertyInfo p in TrialStatsProps) //(string csvColName in statsColsMap.Keys)
                                            //{
                                            //    IEnumerable<double> colValues = getCompletedNonoutlierPropValues(p); //nonOutlierTrials.Select(t => getTrialDoubleValue(t, p));  // trialsWithNameAndTargetValue.Select(t => GetTrialValue(csvColName, t));
                                            //    string csvColName = propertyStringMappings[p];
                                            //    table.SetCurrentRowCol(csvColName, MathExpert.GetStandardDeviation(colValues));

                                            //    statsGroup.Std[p] = MathExpert.GetStandardDeviation(colValues);
                                            //}
                                            ////return nonOutliers;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return allStatsGroups;
        }

        private static void HandleDimsConcatCollapseTrialsSpecialCase(List<TrialSession> sessions)
        {
            ///// Concatenation of dims concat trials
            //var toRemove = new List<Trial>();
            var DIMS_CONCAT_POSTFIX = "DimsConcatenation";
            var trialIsDimsConcat = new Func<Trial, bool>(t => t.GroupName.EndsWith(DIMS_CONCAT_POSTFIX));
            var e1Sesssions = sessions.Where(s => s.Type == TrialSessionType.E1);
            const string AVERAGED_POSTFIX = "Averaged";
            foreach (var e1SessionsByName in e1Sesssions.GroupBy(s => s.Name))
            {
                var trials = e1SessionsByName.SelectMany(s => s.Trials);
                //var targetValues = trials.Select(t => t.TargetValue).Distinct();

                foreach (var trialsByProjectionMode in trials.GroupBy(t => t.ProjectionMode))
                {

                    foreach (var trialsByTarget in trialsByProjectionMode.GroupBy(t => t.TargetValue))
                    {
                        var target = trialsByTarget.Key;

                        var concatDimsTrials = trialsByTarget.Where(trialIsDimsConcat).ToList();

                        //toRemove.Add(concatDimsTrials);
                        if (concatDimsTrials.Count > 0)
                        {
                            var toKeep = concatDimsTrials.First();

                            //var skod = new Dictionary<Func<Trial, double>, Action<Trial, double>>
                            //{
                            //    {  (t) => t.DurationSeconds,
                            //       (t,d) => {
                            //            toKeep.StartTime = DateTime.Now;
                            //            toKeep.EndTime = toKeep.StartTime.AddSeconds(d);
                            //        }
                            //    },
                            //    {  (t) => t.OvershootTravelRatio,
                            //       (t,d) => t.OvershootTravelRatio = d
                            //    //(t) => t.TouchCount,
                            //    //(t) => t.InertialDistanceTravleedRatio,
                            //};

                            // handle duration seconds
                            var averagedSeconds = concatDimsTrials.Sum(t => t.DurationSeconds) / concatDimsTrials.Count;
                            toKeep.StartTime = DateTime.Now;
                            toKeep.EndTime = toKeep.StartTime.AddSeconds(averagedSeconds);

                            // handle overshoot distance
                            toKeep.InertialDistanceTraveled = concatDimsTrials.Sum(t => t.InertialDistanceTraveled);// / concatDimsTrials.Count;
                            toKeep.NoninertialDistanceTraveled = concatDimsTrials.Sum(t => t.NoninertialDistanceTraveled);// / concatDimsTrials.Count;
                            toKeep.OvershootTravelDistance = concatDimsTrials.Sum(t => t.OvershootTravelDistance);/// concatDimsTrials.Count;

                            // handle touch count
                            //toKeep.InertialDistanceTraveled = concatDimsTrials.Sum(t => t.TouchCount) / concatDimsTrials.Count;
                            var averagedTouchCount = concatDimsTrials.Sum(t => t.TouchCount) / concatDimsTrials.Count;
                            toKeep.TouchCount = averagedTouchCount;


                            ////

                            toKeep.GroupName += AVERAGED_POSTFIX;
                            toKeep.Mode = TrialMode.TwoDimensional;

                        }
                        //foreach (Trial trial in concatDimsTrials)
                        //    trial.GroupName += AVERAGED_POSTFIX;

                        //foreach (var trialsOfName in concatDimsTrials.GroupBy(t => t.GroupName))
                        //{
                        //    string trialName = trialsOfName.Key;

                        //    session.Trials.RemoveAll(trialIsDimsConcat);

                        //}
                        //var trialNames = session.Trials.Select()
                    }
                }
            }
            foreach (var session in e1Sesssions)
                session.Trials.RemoveAll(t => t.GroupName.EndsWith(DIMS_CONCAT_POSTFIX));
            //////
        }

        private void SetTrialsFinalOffscreenLocationRelativeToInputRecit(List<TrialSession> sessions)
        {
            foreach (TrialSession session in sessions)
            {
                var rect = session.Space.Offscreen;
                var origo = rect.Origo;
                var axis = new
                {
                    x = rect.OrigoToRight.Normalize(),
                    y = rect.OrigoToBottom.Multiply(-1).Normalize(),
                    z = rect.NormalVector.Normalize()
                };

                foreach (var trial in session.Trials)
                {
                    if (trial.FinalOffscreenLocation != null)
                        trial.FinalOffscreenLocationWRTDevice = new XYZPoint()
                        {
                            X = axis.x.Dot(trial.FinalOffscreenLocation.Subtract(origo)),
                            Y = axis.y.Dot(trial.FinalOffscreenLocation.Subtract(origo)),
                            Z = axis.z.Dot(trial.FinalOffscreenLocation.Subtract(origo)),
                        };
                }
            }
        }

        private void SetTrialsExp3Target(List<TrialSession> sessions)
        {
            foreach (TrialSession session in sessions)
                foreach (var trial in session.Trials)
                {
                    trial.FromOrigoToExp3ReleasePosition = AppSettings.Exp3PointRemapper.FromModeledToNormalSpace(
                        XYPoint.FromPoint(trial.FromOrigoToReleasePosition)
                        );
                    //trial.FromOrigoToExp3TargetPosition = AppSettings.Exp3PointRemapper.FromNormalToModeledSpace(
                    //    XYPoint.FromPoint( trial.FromOrigoToTargetPosition)
                    //    );
                    //if (trial.FromOrigoToExp3TargetPosition == null) { }
                }
        }

        private static string propOutlierColName(string colName) { return colName + OUTLIER_POSTFIX; }

        private static string propConfidenceColName(string colName) { return colName + "Confidence"; }


        private MyDataTable StatGroupsToTableWithStatsRows(Func<Trial, TrialSession> getSessionByTrial, IEnumerable<StatsGroup> statsGroups)
        {
            //DataTable table = new DataTable();
            var table = new MyDataTable(); //new Dictionary<string, List<object>>();
                                           //table.CreateColIfNotExists("confidence");
            CreateTableColsFromSessionAndTrialPropertyNames(table);
            //IEnumerable<string> colNames = table.ColNames; //table.Columns.Cast<DataColumn>();


            //var statsTrials = new List<Trial>();
            var AddInitializedStatsRow = new Action<string, StatsGroup>((statsOperationName, statsGroup) =>
            {
                table.AddRow();

                WriteSesssionAndTrialCsvPropertiesToCurrentTableRow(table,
                            new TrialSession(statsOperationName, statsGroup.SessionType),
                            new Trial
                            {
                                GroupName = statsGroup.GroupName,
                                ProjectionMode = statsGroup.ProjectionMode,
                                Mode = statsGroup.TrialMode,
                                MoveBullsEyeInsteadOfMap = statsGroup.IsEstimationMode,
                                TargetValue = statsGroup.TargetValue,
                                IsStatistic = true,
                                UIAngle = statsGroup.UIAngle,
                                //FromOrigoToExp3TargetPosition = statsGroup.Exp3TargetPosition,
                                FromOrigoToExp3ReleasePosition = statsGroup.Exp3ReleasePosition,
                                IsExp3SpaceToBeUsed = statsGroup.IsExp3SpaceToBeUsed,
                                FromOrigoToTargetPosition = statsGroup.TargetLocation //completedStatsGroupTrials.First().FromOrigoToTargetPosition // shuold be the3 same for all in target value and angle combi
                            }
                            );
            });



            foreach (StatsGroup statsGroup in statsGroups)
            {
                foreach (Trial trial in statsGroup)
                {
                    table.AddRow();
                    WriteSesssionAndTrialCsvPropertiesToCurrentTableRow(table, getSessionByTrial(trial), trial);

                    foreach (PropertyInfo p in trial.OutlierProps)
                    {
                        string csvColName = propertyStringMappings[p];
                        string csvPropOutlierIndicatorColName = propOutlierColName(csvColName);
                        table.CreateColIfNotExists(csvPropOutlierIndicatorColName, 0);
                        table.SetCurrentRowCol(csvPropOutlierIndicatorColName, 1);
                    }
                }

                //var session = getSessionByTrial(trial);


                foreach (PropertyInfo p in statsGroup.PropertyStats.Keys)
                {
                    string csvColName = propertyStringMappings[p];
                    table.CreateColIfNotExists(propOutlierColName(csvColName), 0);
                    table.CreateColIfNotExists(propConfidenceColName(csvColName), 0);
                }

                AddInitializedStatsRow(MEAN_SESSION_NAME, statsGroup);
                foreach (PropertyInfo p in statsGroup.PropertyStats.Keys)
                {
                    var stats = statsGroup.PropertyStats[p];
                    string csvColName = propertyStringMappings[p];
                    table.SetCurrentRowCol(csvColName, stats.mean);

                    bool isConfidenceColumnToBeCreated = GetAttribute<CsvDoStatsAttribute>(p).Confidence;
                    if (isConfidenceColumnToBeCreated)
                    {
                        string csvConfidenceColName = propConfidenceColName(csvColName);// + "Confidence";
                        table.CreateColIfNotExists(csvConfidenceColName);
                        table.SetCurrentRowCol(csvConfidenceColName, stats.confidence);
                    }
                }

                //AddInitializedStatsRow(STD_SESSION_NAME, statsGroup);
                //foreach (PropertyInfo p in statsGroup.PropertyStats.Keys)
                //{
                //    var stats = statsGroup.PropertyStats[p];
                //    string csvColName = propertyStringMappings[p];

                //    table.SetCurrentRowCol(csvColName, stats.std);
                //}


                //AddInitializedStatsRow("std");
                //string csvColName = propertyStringMappings[p];


            }

            //log.Info("Outlier detectoin skips: {0}/{1}", outlierDetectionIterationsSkipped, outlierDetectionIterationsSkipped + outlierDetectionIterationsCompleted);


            table.CreateColIfNotExists("finalizer");
            return table;
        }

        private static void DuplicateDerivedTrials(List<TrialSession> sessions)
        {
            var duplicationAttrs = typeof(Trial).GetTypeInfo().GetCustomAttributes<DuplicateTrialAttribute>();
            var converters = duplicationAttrs.Select(
                a => new Func<Trial, Trial>(
                    (Trial source) =>
                    {
                        // apply filters
                        if (a.IsTrialModeFilterEnabled && a.TrialModeFilter != source.Mode)
                            return null;

                        Trial clone = StringExpert.CloneByJson(source);
                        clone.IsDuplication = true;

                        // apply overrides
                        if (a.IsTrialModeOverrideEnabled)
                            clone.Mode = a.TrialModeOverride;

                        if (a.TrialGroupNameAppendToProjectionMode != null)
                            clone.GroupName = clone.ProjectionMode + a.TrialGroupNameAppendToProjectionMode;

                        return clone;
                    }
                    ));


            foreach (var session in sessions)
            {
                var clonedTrials = converters.SelectMany(
                    converter => session.Trials.Select(t => converter(t))
                    ).Where(t => t != null).ToList();
                if (clonedTrials.Count > 0)
                    session.Trials.AddRange(clonedTrials);
            }
        }

        private static void WriteSesssionAndTrialCsvPropertiesToCurrentTableRow(MyDataTable table, TrialSession sessionOfType, Trial trial)
        {
            foreach (var sessionProp in SessionProps)
            {
                table.SetCurrentRowCol(
                    propertyStringMappings[sessionProp],
                    sessionProp.GetValue(sessionOfType)
                    );
            }

            foreach (var trialProp in TrialProps)
            {
                table.SetCurrentRowCol(
                    propertyStringMappings[trialProp],
                    trialProp.GetValue(trial)
                    );
            }
        }

        private void CreateTableColsFromSessionAndTrialPropertyNames(MyDataTable table)
        {
            //foreach (var sessionProp in SessionProps)
            //    table["session" + sessionProp.Name] = new List<object>();
            ////table.Columns.Add("session" + sessionProp.Name);
            //foreach (var trialProp in TrialProps)
            //    table["trial" + trialProp.Name] = new List<object>();
            ////table.Columns.Add("trial" + trialProp.Name);
            //foreach (var csvColName in propertyStringMappings.Values)
            foreach (var prop in propertyStringMappings.Keys)
            {
                string csvColName = propertyStringMappings[prop];
                bool isIndex = PropHasAttribute<CsvIsIndexAttribute>(prop);

                table[csvColName] = new List<object>();
                if (isIndex)
                    table.SortIndex = table.GetColIndex(csvColName);
            }
        }

        private const string csvColSeparator = "\t";


        private async Task WriteTableToFile(string fileprefix, MyDataTable table)
        {

            var header = table.ColNames.Aggregate((s1, s2) => s1 + csvColSeparator + s2);
            var lines = new List<string> { header };

            var sortedRows = table.PopAllSortByIndex();
            foreach (var row in sortedRows)
                lines.Add(
                    row.Select(
                        v => ConvertValue(v).ToString()
                        ).Aggregate(
                            (s1, s2) => s1 + csvColSeparator + s2
                            )
                    );

            //var row = table.PopFirst();
            //while (row != null)
            //{
            //    lines.Add(
            //        row.Select(
            //            v => ConvertValue(v).ToString()
            //            ).Aggregate(
            //                (s1, s2) => s1 + sep + s2
            //                )
            //        );

            //    row = table.PopFirst();
            //}

            string filePrefix = "(" + fileprefix + ")";
            string filename = "TrialSesssions-" + filePrefix + ".csv";

            try
            {
                await WriteCsvDataFile(lines, filename);
            }
            catch (Exception ex)
            {
                new Windows.UI.Popups.MessageDialog("Failed to write file (" + filename + "):" + ex.Message).ShowAsync();
            }
        }

        private async Task WriteCsvDataFile(IEnumerable<string> lines, string filename)
        {
            var file = await csvFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteLinesAsync(file, lines);
        }

        //private static double CalculateMedian(IEnumerable<TrialSession> sessions)
        //{
        //    var allDurationSeconds = sessions.SelectMany(s => s.Trials).Select(t => t.DurationSeconds).ToList();
        //}

        private static IEnumerable<T> GetColValues<T>(DataTable t, DataColumn col)
        {
            foreach (DataRow r in t.Rows)
                yield return (T)r[col];
        }

        private object ConvertValue(object v)
        {
            if (v == null)
                return CsvNullValueString;

            if (v is double && double.IsNaN((double)v))
                return "0.0";

            Type t = v.GetType();

            if (t == typeof(TimeSpan))
                return ((TimeSpan)v).TotalMilliseconds;

            if (t == typeof(DateTime))
                return string.Format("{0:s}", ((DateTime)v));

            if (t == typeof(bool))
                return (bool)v ? 1 : 0;

            return v;
        }

    }

}
