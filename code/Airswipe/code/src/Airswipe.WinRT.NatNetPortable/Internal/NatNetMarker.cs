using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Airswipe.WinRT.NatNetPortable
{
    internal class NatNetMarker : Marker
    {
        #region Constructors

        private NatNetMarker() { }

        public static NatNetMarker Create(NatNetML.Marker m)
        {
            return new NatNetMarker
            {
                ID = m.ID,
                X = m.x,
                Y = m.y,
                Z = m.z,    
                Size = m.size,
            };
        }

        #endregion
        #region Poperties

        public int ID { get; private set; }

        public float Size { get; private set; }

        public float X { get; private set; }

        public float Y { get; private set; }

        public float Z { get; private set; }

        #endregion
    }
}
