using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Airswipe.WinRT.Core.MotionTracking;
using System.Collections;
using Newtonsoft.Json.Converters;

namespace Airswipe.WinRT.Core.Data.Dto
{
    public abstract class TrialSessionBase : JsonObject
    {
        //private const int TRIAL_MODE_DUPLICATION_COUNT = 5;

        protected static readonly IEnumerable<ProjectionMode> AllProjectionModes = new List<ProjectionMode> {
            ProjectionMode.Baseline,
            ProjectionMode.Directional,
            ProjectionMode.PlaneNormal,
            ProjectionMode.Spherical
        };
         
            //Enum.GetValues(typeof(ProjectionMode));
        //new List<ProjectionMode> { ProjectionMode.Baseline };

        protected static readonly IEnumerable AllTrialModes =
            Enum.GetValues(typeof(TrialMode));
        //new List<TrialMode> { TrialMode.TwoDimensional };
        //new List<TrialMode> { TrialMode.Estimation};

        const int MIN_TARGET = -50;
        const int MAX_TARGET = 50;
        const int STEP_SIZE = 10;

        public static List<int> TrialTargetValues = new List<int> { -50, -40, -30, -20, -10, 10, 20, 30, 40, 50 }; //GetRange(MIN_TARGET, MAX_TARGET, STEP_SIZE).ToList();

        protected int TRIAL_COUNT = TrialTargetValues.Count;

        public Guid UID = Guid.NewGuid();
        private DateTime createTime = DateTime.Now;

        private TrialSessionType type;


        //protected List<double> angles;

        protected Random random;

        #region Constructor 

        public TrialSessionBase(string name, TrialSessionType type)
        {
            //CreateTime = DateTime.Now;
            Name = name;
            Type = type;

            Trials = new List<Trial>();

            var nameCharCodes = name.Select(ch => (int)ch).ToList();
            int seed = (int)nameCharCodes.Aggregate((d1, d2) => d1 + d2);

            //InitializeAngles();

            random = new Random(seed);

            Trials.AddRange(GenerateTrials(Type));
        }

        //private void InitializeAngles()
        //{
        //    angles = new List<double>();
        //    double angleStep = Math.PI / (TRIAL_COUNT + 1); // +1 to avoid horisontal angles
        //    for (var i = 1; i <= TRIAL_COUNT; i++)
        //        //for (double angle = angleStep; angle < Math.PI; angle += angleStep)
        //        angles.Add(i * angleStep);
        //}

        protected class TrialProjectionMode
        {
            public TrialMode TrialMode;
            public ProjectionMode ProjectionMode;
            internal bool MoveBullsEyeInsteadOfMap;
        }

        private IEnumerable<int> GetRange(int min, int max, int step)
        {
            if ((max - min) % step != 0)
                throw new Exception("Not a valid param combination for range");

            for (int i = min; i <= max; i += step)
                yield return i;
        }

        protected abstract IEnumerable<Trial> GenerateTrials(TrialSessionType type);

        public static void RandomizeList<T>(IList<T> list, Random random)
        {
            int s = list.Count;
            while (s > 1)
            {
                s--;
                int r = random.Next(1 + s);

                T value = list[r];
                list[r] = list[s];
                list[s] = value;
            }
        }

        public static void RandomizeListNonTyped(IList list, Random random)
        {
            int s = list.Count;
            while (s > 1)
            {
                s--;
                int r = random.Next(1 + s);

                object value = list[r];
                list[r] = list[s];
                list[s] = value;
            }
        }

        //public void EndTrialAndAdvance()
        //{
        //    FirstIncompleteTrial.EndTime = DateTime.Now;
        //}


        #endregion
        #region Properties

        public string Name { get; set; }

        [CsvIgnore]
        public List<Trial> Trials { get; set; }

        public DateTime CreateTime
        {
            get { return createTime; }
            set { createTime = value; }
        }

        public bool IsSessionCompleted
        {
            get { return CompletionRate == 1; }
        }

        public double CompletionRate
        {
            get { return Trials.Where(t => t.IsCompleted).Count() / (double)Trials.Count; }
        }

        [CsvIgnore]
        public bool IsStarted
        {
            get { return CompletionRate > 0; }
        }

        //[CsvIgnore]
        //public DateTime StartTime
        //{
        //    get; set;
        //}

        //[CsvIgnore]
        //public DateTime CompletionTime
        //{
        //    get; set;
        //}

        [CsvIgnore]
        public TimeSpan Duration
        {
            get {
                //var completedTrials = Trials.Where(t => t.IsCompleted);
                var earliestStartTime  = Trials.Where(t => t.IsStarted).Select(t => t.StartTime).Min();
                var latestEndTime = Trials.Where(t => t.IsCompleted).Select(t => t.StartTime).Max();

                return latestEndTime - earliestStartTime;
            }
        }

        [CsvIgnore]
        [JsonIgnore]
        public Trial FirstIncompleteTrial
        {
            get { return Trials.Where(t => !t.IsCompleted).FirstOrDefault(); }
        }

        [CsvIgnore]
        [JsonIgnore]
        public string Filename
        {
            get
            {
                return string.Format(
                    "{0:yyyy-MM-dd_hh-mm-ss}-{1}-{2}.json",
                    CreateTime,
                    Type,
                    string.IsNullOrEmpty(Name) ? "unnamed" : Name
                    );
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public TrialSessionType Type
        {
            get { return type; }
            set { type = value; }
            //set { type = (value == TrialSessionType.TrialSession) ? TrialSessionType.E1 : value; }
        }

        [CsvIgnore]
        public InputSpace Space { get; set; }

        #endregion
    }
}
