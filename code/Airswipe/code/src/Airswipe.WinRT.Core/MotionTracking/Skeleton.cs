using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public interface Skeleton
    {
        #region Poperties

        int ID { get; }

        string Name { get; }

        //int NRigidBodies { get; }

        IList<RigidBody> RigidBodies { get; }

        #endregion
    }
}
