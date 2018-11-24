using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.NatNetPortable
{
    public class NatNetMarkerSet : NatNetDataDescriptor, MarkerSet
    {
        #region Fields

        internal NatNetML.MarkerSet markerSet;

        private IList<string> markerNames;

        #endregion
        #region Constructors

        protected NatNetMarkerSet(NatNetML.MarkerSet markerSet)
            : base(markerSet)
        {
            this.markerSet = markerSet;
        }

        public static NatNetMarkerSet Create(NatNetML.MarkerSet markerSet)
        {
            return new NatNetMarkerSet(markerSet);
        }

        #endregion
        #region Properties

        public IList<string> MarkerNames
        {
            get
            {
                if (markerNames == null)
                    markerNames = new ArraySegment<string>(markerSet.MarkerNames, 0, NMarkers);

                return markerNames;
            }
        }

        public string Name { get { return markerSet.Name; } }

        public int NMarkers { get { return markerSet.nMarkers; } }

        #endregion
    }
}
