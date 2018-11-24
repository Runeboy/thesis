using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.NatNetPortable
{
    internal class NatNetRigidBody : NatNetDataDescriptor, RigidBody
    {
        #region Fields

        private NatNetML.RigidBody rigidBody;

        #endregion
        #region Constructors

        protected NatNetRigidBody(NatNetML.RigidBody rigidBody) : base(rigidBody) {
            this.rigidBody = rigidBody;
        }

        public static NatNetRigidBody Create(NatNetML.RigidBody rigidBody)
        {
            return new NatNetRigidBody(rigidBody);
        }

        #endregion
        #region Poperties

        public int ID { get { return rigidBody.ID; } }

        public string Name { get { return rigidBody.Name; } }

        public float Offsetx { get { return rigidBody.offsetx; } }
        
        public float Offsety { get { return rigidBody.offsety; } }
        
        public float Offsetz { get { return rigidBody.offsetz; } }
        
        public int ParentID { get { return rigidBody.parentID; } }

        #endregion
    }
}
