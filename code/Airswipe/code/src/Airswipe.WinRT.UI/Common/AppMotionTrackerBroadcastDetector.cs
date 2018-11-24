using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.NatNetPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.UI.Common
{
    public class AppMotionTrackerBroadcastDetector : NatNetMotionBroadcastDetector
    {
        #region Fields

        private static MotionBroadcastDetector instance;

        #endregion
        #region Constructors 

        public AppMotionTrackerBroadcastDetector() { }

        public static void InitializeInstance()
        {
            instance = new AppMotionTrackerBroadcastDetector();
        }

        #endregion
        #region Properties

        public static MotionBroadcastDetector Instance {
            get
            {
                if (instance == null)
                    throw new Exception("Not initialized");

                return instance;
            }
        }


        #endregion
    }
}
