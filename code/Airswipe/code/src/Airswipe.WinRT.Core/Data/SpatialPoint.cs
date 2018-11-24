namespace Airswipe.WinRT.Core.Data
{
    public interface SpatialPoint : PointLocation<SpatialPoint>
    {
        double X { get; }
        double Y { get; }
        double Z { get; }

        string ToShortString();

        SpatialPoint Cross(SpatialPoint p);

        //double Length { get; }

     //   SpatialPoint Add(SpatialPoint p);

        //SpacialPoint Subtract(SpacialPoint p);

        //SpacialPoint Multiply(double f);

        //double Dot(SpacialPoint p);

        //ProjectedSpacialPoint Project(SpacialPlane plane);

        //SpatialPoint Normalize();
    }
}

