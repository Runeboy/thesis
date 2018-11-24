namespace Airswipe.WinRT.Core.MotionTracking
{
    public interface RigidBodyData : Marker
    {
        #region Poperties

        //int ID { get; }

        float Qw { get; }

        float Qx { get; }

        float Qy { get;  }

        float Qz { get;  }

        bool Tracked { get; }

        //float X { get;  }

        //float Y { get;  }

        //float Z { get; }

        Marker[] Markers { get; }

        #endregion
    }
}
