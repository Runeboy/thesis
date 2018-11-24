using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.NatNetPortable
{
    internal class NatNetRigidBodyData : RigidBodyData, Marker // implement Marker interface for convenience when comparing positions with markers
    {
        //#region Fields

        //private NatNetML.RigidBody rigidBody;

        //#endregion
        #region Constructors

        private NatNetRigidBodyData() { }

        public static NatNetRigidBodyData Create(NatNetML.RigidBodyData r)
        {
            return new NatNetRigidBodyData
            {
                ID = r.ID,
                Qw = r.qw,
                Qx = r.qx,
                Qy = r.qy,
                Qz = r.qz,
                Tracked = r.Tracked,
                X = r.x,
                Y = r.y,
                Z = r.z,
                Markers = r.Markers.Take(r.nMarkers).Select(m => NatNetMarker.Create(m)).ToArray()
            };
        }

        #endregion
        #region Poperties

        public int ID { get; private set; }

        public float Qw { get; private set; }

        public float Qx { get; private set; }

        public float Qy { get; private set; }

        public float Qz { get; private set; }

        public bool Tracked { get; private set; }

        public float X { get; private set; }

        public float Y { get; private set; }

        public float Z { get; private set; }

        public Marker[] Markers { get; private set; }

        #endregion
    }
}
