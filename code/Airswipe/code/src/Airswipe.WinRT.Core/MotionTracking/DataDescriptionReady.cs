using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public delegate void DataDescriptionReady(IList<MarkerSet> markerSets, IList<RigidBody> rigidBodies, IList<Skeleton> skeletons);
}
