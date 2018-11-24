using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.NatNetPortable
{
    internal class NatNetServerDescription : ServerDescription
    {
        #region Fields

        internal NatNetML.ServerDescription serverDescription;

        #endregion
        #region Constructors

        public NatNetServerDescription()
        {
            serverDescription = new NatNetML.ServerDescription();
        }

        #endregion
        #region Properties

        public int[] HostAppVersion
        {
            get { return serverDescription.HostAppVersion; }
        }

        public int[] NatNetVersion
        {
            get { return serverDescription.NatNetVersion; }
        }

        public string HostApp
        {
            get { return serverDescription.HostApp; }
        }
        
        #endregion
    }
}
