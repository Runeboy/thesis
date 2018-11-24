using Airswipe.WinRT.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public static class MotionTrackerClientSettings
    {
        #region Properties

        public static string SelectedLocalNetworkAddress
        {
            get { return StorageExpert.GetSetting("SelectedLocalNetworkAddress"); }
            set { StorageExpert.SaveSettings("SelectedLocalNetworkAddress", value); }
        }

        public static string SelectedServerNetworkAddress
        {
            get { return StorageExpert.GetSetting("SelectedServerNetworkAddress"); }
            set { StorageExpert.SaveSettings("SelectedServerNetworkAddress", value); }
        }

        public static string SelectedConnectionTypeString
        {
            get { return StorageExpert.GetSetting("SelectedConnectionType"); }
            set { StorageExpert.SaveSettings("SelectedConnectionType", value); }
        }

        public static IEnumerable<string> NetworkAddressHistory
        {
            get
            {
                var stored = StorageExpert.GetSetting("NetworkAddressHistory");
                return (stored == null) ? null : stored.Split(',');
            }
            set { StorageExpert.SaveSettings("NetworkAddressHistory", StringExpert.CommaSeparate(value)); }
        }

        #endregion
    }
}
