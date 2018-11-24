namespace Airswipe.WinRT.Core.Data
{
    public interface PlanePoint : PointLocation<PlanePoint>
    {
        double X { get; set; }
        double Y { get; set; }

        //double DistanceFrom(TwoDimensionalPoint p);
    }
}
