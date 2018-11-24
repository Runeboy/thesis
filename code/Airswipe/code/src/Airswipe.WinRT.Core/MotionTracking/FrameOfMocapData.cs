namespace Airswipe.WinRT.Core.MotionTracking
{
    /// <summary>
    /// Models (a relevant subset of) a data frame
    /// </summary>
    public interface FrameOfMocapData
    {
        int iFrame { get; set; }

        float fLatency { get; }

        double fTimestamp { get; }

        bool bRecording { get; }

        uint Timecode { get; }

        uint TimecodeSubframe { get; }

        RigidBodyData[] RigidBodies { get; }

        Marker[] OtherMarkers { get; }
    }
}
