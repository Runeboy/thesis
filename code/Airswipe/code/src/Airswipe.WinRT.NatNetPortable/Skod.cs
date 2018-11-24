using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.NatNetPortable
{
    public class Skod : FrameOfMocapData
    {
        public int iFrame { get; set; }

        public float fLatency { get; set; }

        public double fTimestamp { get; set; }

        public bool bRecording { get; set; }

        public uint Timecode { get; set; }

        public uint TimecodeSubframe { get; set; }

    };
}
