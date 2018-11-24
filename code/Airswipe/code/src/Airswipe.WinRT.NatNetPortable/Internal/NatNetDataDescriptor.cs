using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.NatNetPortable
{
    internal abstract class NatNetDataDescriptor : Airswipe.WinRT.Core.MotionTracking.DataDescriptor
    {
        #region Fields

        internal NatNetML.DataDescriptor dataDescriptor;

        #endregion
        #region Constructors

        protected NatNetDataDescriptor(NatNetML.DataDescriptor dataDescriptor) 
        {
            this.dataDescriptor = dataDescriptor;
        }

        public static NatNetDataDescriptor Create(NatNetML.DataDescriptor dataDescriptor)
        {
            switch (dataDescriptor.type)
            {
                case (int)NatNetDataDescriptorType.eMarkerSetData: return NatNetMarkerSet.Create((NatNetML.MarkerSet)dataDescriptor);
                case (int)NatNetDataDescriptorType.eRigidbodyData: return NatNetRigidBody.Create((NatNetML.RigidBody)dataDescriptor);
                case (int)NatNetDataDescriptorType.eSkeletonData : return NatNetSkeleton.Create((NatNetML.Skeleton)dataDescriptor);
            }

            throw new ArgumentException("Handling of type " + dataDescriptor.type + " not implemented.");
        }

        #endregion
        #region Properties

        public int Type 
        {
            get { return dataDescriptor.type; }
        }

        #endregion
    }
}
