namespace Airswipe.WinRT.Core.MotionTracking
{
    public interface Marker
    {
        int ID { get; }

        //float Size { get; }

        float X { get; }

        float Y { get; }

        float Z { get; }

    }
}
