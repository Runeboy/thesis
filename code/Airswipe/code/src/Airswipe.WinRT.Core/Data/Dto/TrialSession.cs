using System.Collections.Generic;
using Airswipe.WinRT.Core.MotionTracking;
using System.Linq;
using System;

namespace Airswipe.WinRT.Core.Data.Dto
{
    public class TrialSession : TrialSessionBase
    {
        public TrialSession(string name, TrialSessionType type) : base(name, type)
        { }

        protected override IEnumerable<Trial> GenerateTrials(TrialSessionType type)
        {
            switch (type)
            {
                case TrialSessionType.TrialSession: return GeneratePilotExpTrials();
                case TrialSessionType.E1: return GenerateE1Trials();
                case TrialSessionType.E2: return GenerateE2Trials();
                case TrialSessionType.E3: return GenerateE3Trials();
            }
            throw new Exception("No trial generation handler for type.");
        }


        protected IEnumerable<Trial> GeneratePilotExpTrials()
        {
            //throw new Exception("Not to be used.");

            var angles = new List<double>();
            double angleStep = Math.PI / (TRIAL_COUNT + 1); // +1 to avoid horisontal angles
            for (var i = 1; i <= TRIAL_COUNT; i++)
                //for (double angle = angleStep; angle < Math.PI; angle += angleStep)
                angles.Add(i * angleStep);


            //const int TRIAL_STEP = (MAX_TARGET  - MIN_TARGET)/TRIAL_COUNT;
            //var targetIntervals = new List<int>() { 15,25,35,45, };

            //var valueEstimationProjectionModes = new List<ProjectionMode> { ProjectionMode. };

            var combinations = new List<TrialProjectionMode>();

            foreach (ProjectionMode projectionMode in AllProjectionModes)
                foreach (TrialMode trialMode in AllTrialModes)
                {
                    combinations.Add(
                        new TrialProjectionMode
                        {
                            ProjectionMode = projectionMode,
                            TrialMode = trialMode
                        }
                        );

                    if (projectionMode != ProjectionMode.Baseline)
                        combinations.Add(
                            new TrialProjectionMode
                            {
                                ProjectionMode = projectionMode,
                                //TrialMode = TrialMode.TwoDimensional,
                                TrialMode = trialMode,
                                MoveBullsEyeInsteadOfMap = true
                            }
                        );
                }

            //// estimation mode
            //foreach (ProjectionMode projectionMode in ProjectionModes)
            //    combinations.Add(
            //        new TrialProjectionMode
            //        {
            //            ProjectionMode = projectionMode,
            //            TrialMode = TrialMode.TwoDimensional,
            //            MoveBullsEyeInsteadOfMap = true
            //        }
            //    );


            //foreach (ProjectionMode projectionMode in ProjectionModes)
            //foreach (TrialMode trialMode in TrialModes)

            foreach (TrialProjectionMode combination in combinations)
            {
                //var values = new List<int>();
                //for (var i = 1; i <= TRIAL_COUNT; i++)
                //{
                //    bool isNegative = (random.NextDouble() < 0.5);
                //    values.Add(
                //        (MIN_TARGET + random.Next(MIN_TARGET + i * TRIAL_STEP)) * (isNegative? -1 : 1)
                //        );
                //}

                RandomizeList(TrialTargetValues, random);
                RandomizeList(angles, random);

                bool useAngle = (combination.TrialMode == TrialMode.TwoDimensional);

                for (var i = 1; i <= TRIAL_COUNT; i++)
                {
                    var t = new Trial
                    {
                        UIAngle = useAngle ? angles[i - 1] : 0,
                        Mode = combination.TrialMode,
                        TargetValue = TrialTargetValues[i - 1],  //random.Next(minTarget, maxTarget),
                        IsOffscreenSpaceEnabled = (combination.ProjectionMode != ProjectionMode.Baseline),
                        ProjectionMode = combination.ProjectionMode,
                        MoveBullsEyeInsteadOfMap = combination.MoveBullsEyeInsteadOfMap
                    };
                    t.GroupName = CreateGroupIdFromTrial(t);

                    yield return t;
                }
            }
        }

        protected IEnumerable<Trial> GenerateE1Trials()
        {
            return GenerateTrials(
                AllProjectionModes,
                new List<TrialMode> { TrialMode.Horisontal, TrialMode.Vertical }
                );
        }

        protected IEnumerable<Trial> GenerateTrials(IEnumerable<ProjectionMode> projectionModes, IEnumerable<TrialMode> trialModes)
        {
            foreach (ProjectionMode projectionMode in projectionModes)
                foreach (TrialMode trialMode in trialModes)
                {
                    RandomizeList(TrialTargetValues, random);

                    foreach (double targetValue in TrialTargetValues)
                    //for (var i = 1; i <= TRIAL_COUNT; i++)
                    {
                        var t = new Trial
                        {
                            UIAngle = (trialMode == TrialMode.Horisontal) ? 0 : Math.PI / 2.0,
                            Mode = trialMode,
                            TargetValue = targetValue, //TrialTargetValues[i - 1],  //random.Next(minTarget, maxTarget),
                            IsOffscreenSpaceEnabled = (projectionMode != ProjectionMode.Baseline),
                            ProjectionMode = projectionMode,
                            MoveBullsEyeInsteadOfMap = false
                        };
                        t.GroupName = CreateGroupIdFromTrial(t);

                        yield return t;
                    }
                }
        }

        protected IEnumerable<Trial> GenerateE2Trials()
        {
            return GenerateE2E3Trials(true);
        }

            //protected IEnumerable<Trial> GenerateE3Trials()
            //{
            //    return GenerateE2E3Trials(false);
            //}

        private static string CreateGroupIdFromTrial(Trial t)
        {
            return string.Format(
                   "{0}-{1}{2}",
                   t.ProjectionMode,
                   t.Mode,
                   (t.MoveBullsEyeInsteadOfMap ? "-Estimation" : "")
                   );
        }

        protected IEnumerable<Trial> GenerateE2E3Trials(bool moveBullsEyeInsteadOfMap)
        {
            var angles = new List<double>();
            const int ANGLE_COUNT_PER_QUADRANT = 3;
            const double ANGLE_STEP = (Math.PI / 2.0) / ANGLE_COUNT_PER_QUADRANT;
            for (double angle = 0; (angle + ANGLE_STEP) < Math.PI; angle += ANGLE_STEP)
            {
                angles.Add(angle);
            }

            RandomizeList(TrialTargetValues, random);
            RandomizeList(angles, random);

            var angleTargetCombinations = angles.SelectMany(a =>
                TrialTargetValues.Select(t => new { angle = a, target = t })
                ).ToList();
            RandomizeListNonTyped(angleTargetCombinations, random);

            foreach (var combination in angleTargetCombinations)
            //foreach (double angle in angles) {
            //    //RandomizeList(angles, random);
            //    //RandomizeList(TrialTargetValues, random);
            //    foreach (double target in TrialTargetValues)
            {
                var t = new Trial
                {
                    UIAngle = combination.angle,
                    Mode = TrialMode.TwoDimensional,
                    TargetValue = combination.target,  //random.Next(minTarget, maxTarget),
                    IsOffscreenSpaceEnabled = true,
                    ProjectionMode = ProjectionMode.Spherical,
                    MoveBullsEyeInsteadOfMap = moveBullsEyeInsteadOfMap
                };
                t.GroupName = CreateGroupIdFromTrial(t);

                yield return t;
            }
            //}

            //throw new NotImplementedException();
        }

        protected IEnumerable<Trial> GenerateE3Trials()
        {
            var angles = new List<double>();
            const int ANGLE_COUNT_PER_QUADRANT = 3;
            const double ANGLE_STEP = (Math.PI / 2.0) / ANGLE_COUNT_PER_QUADRANT;
            for (double angle = 0; (angle + ANGLE_STEP) < Math.PI; angle += ANGLE_STEP)
            {
                angles.Add(angle);
            }

            List<int> targets = TrialTargetValues.Where(t => Math.Abs(t) != 50).ToList();

            RandomizeList(targets, random);
            RandomizeList(angles, random);

            var angleTargetCombinations = angles.SelectMany(a =>
                targets.Select(t => new { angle = a, target = t })
                ).ToList();
            RandomizeListNonTyped(angleTargetCombinations, random);

            foreach (bool isToUseExp3Space in new bool[] { false, true })
                foreach (var combination in angleTargetCombinations)
                //foreach (double angle in angles) {
                //    //RandomizeList(angles, random);
                //    //RandomizeList(TrialTargetValues, random);
                //    foreach (double target in TrialTargetValues)
                {
                    var t = new Trial
                    {
                        UIAngle = combination.angle,
                        Mode = TrialMode.TwoDimensional,
                        TargetValue = combination.target,  //random.Next(minTarget, maxTarget),
                        IsOffscreenSpaceEnabled = true,
                        ProjectionMode = ProjectionMode.Spherical,
                        //MoveBullsEyeInsteadOfMap = true,
                        IsExp3SpaceToBeUsed = isToUseExp3Space
                    };
                    t.GroupName = CreateGroupIdFromTrial(t);

                    yield return t;
                }
            //}

            //throw new NotImplementedException();
        }

    }
}
