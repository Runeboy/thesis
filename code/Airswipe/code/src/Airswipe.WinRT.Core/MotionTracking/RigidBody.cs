using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public interface RigidBody
    {
        #region Poperties

        int ID { get; }

        string Name { get; }

        float Offsetx { get; }

        float Offsety { get; }

        float Offsetz { get; }

        int ParentID { get; }

        #endregion

    }
}
