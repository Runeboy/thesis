using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.NatNetPortable
{
    public class NatNetFrameOfMocapData : FrameOfMocapData
    {
        #region Fields

        private NatNetML.FrameOfMocapData frameOfMocapData;

        #endregion
        #region Constructors

        //public FrameOfMocapData()
        //{

        //}

        public static NatNetFrameOfMocapData Create(NatNetML.FrameOfMocapData data)
        {
                //frameOfMocapData = data 
            return new NatNetFrameOfMocapData { 
                iFrame = data.iFrame,
                fLatency = data.fLatency,
                fTimestamp = data.fTimestamp,
                bRecording = data.bRecording,
                Timecode = data.Timecode,
                TimecodeSubframe = data.TimecodeSubframe
            };
        }

        #endregion
        #region Properties

        //public int iFrame { get; private set; }

        //public float fLatency { get; private set; }

        //public double fTimestamp { get; private set; }

        //public bool bRecording { get; private set; }

        //public uint Timecode { get; private set; }

        //public uint TimecodeSubframe { get; private set; }

        public int iFrame { get; set; }

        public float fLatency { get; private set; }

        public double fTimestamp { get; private set; }

        public bool bRecording { get; private set; }

        public uint Timecode { get; private set; }

        public uint TimecodeSubframe { get; private set; }

        #endregion
    }
}
