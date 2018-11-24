using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.NatNetPortable
{
    public class NatNetSkeleton : NatNetDataDescriptor, Skeleton
    {
        #region Fields

        private NatNetML.Skeleton skeleton;

       private IList<RigidBody> rigidBodies;

        #endregion
        #region Constructors

        protected NatNetSkeleton(NatNetML.Skeleton skeleton) : base(skeleton) 
        {
            this.skeleton = skeleton;
        }

        public static NatNetSkeleton Create(NatNetML.Skeleton skeleton)
        {
            return new NatNetSkeleton(skeleton);
        }

        #endregion
        #region Poperties

        public int ID { get { return skeleton.ID; } }

        public string Name { get { return skeleton.Name; } }

        public int NRigidBodies { get { return skeleton.nRigidBodies; } }

        public IList<RigidBody> RigidBodies
        {
            get
            {
                if (rigidBodies == null)
                    rigidBodies = skeleton.RigidBodies.Take(NRigidBodies).Select<NatNetML.RigidBody, RigidBody>(
                        r => NatNetRigidBody.Create(r)
                        ).ToArray();

                return rigidBodies;
            }
        }

        #endregion
    }
}
