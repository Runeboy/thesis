namespace Airswipe.WinRT.Core.Data
{
    public interface SpatialPlane
    {
        //SpacialPoint PlanePoint { get; set; }
        //SpacialPoint PlanePoint { get; set; }

        SpatialPoint Intersects { get; }

        SpatialPoint FirstIntersect { get; }

        double Offset  { get; set; }

        SpatialPoint NormalUnitVector { get; }

        SpatialPoint NormalVector { get; set; }

        bool ContainsPoint(SpatialPoint p);

        bool ContainsPointApprox(SpatialPoint p);

        bool ContainsPoint(SpatialPoint p, double grace);
    }
}
