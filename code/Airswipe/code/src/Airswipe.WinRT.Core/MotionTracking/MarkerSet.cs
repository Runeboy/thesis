using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public interface MarkerSet
    {
        #region Properties

        IList<string> MarkerNames { get; }

        string Name { get; }

        //int NMarkers { get; }

        #endregion
    }
}
