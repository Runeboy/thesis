using System;

namespace Airswipe.WinRT.Core.Data.Dto
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DuplicateTrialAttribute :Attribute
    {
        private TrialMode trialModeFilter;
        private TrialMode trialModeOverride;


        public DuplicateTrialAttribute()
        {
        }

        public string TrialGroupNameAppendToProjectionMode { get; set; }

        public bool IsTrialModeFilterEnabled { get; private set; }

        public bool IsTrialModeOverrideEnabled { get; private set; }


        public TrialMode TrialModeFilter {
            get { return trialModeFilter; }
            set {
                trialModeFilter = value;
                IsTrialModeFilterEnabled = true;
            }
        }

        public TrialMode TrialModeOverride {
            get { return trialModeOverride;  }
            set {
                trialModeOverride = value;
                IsTrialModeOverrideEnabled = true;
            }
        }

        //public Func<Trial,Trial> DuplicateProcessor { get;  set; }
    }
}
