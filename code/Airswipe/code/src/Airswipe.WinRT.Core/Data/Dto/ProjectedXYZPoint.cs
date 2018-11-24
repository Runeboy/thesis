using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Airswipe.WinRT.Core.Data.Dto
{
    public struct ProjectedXYZPoint : ProjectedSpatialPoint
    {
        #region Constructors

        public static ProjectedXYZPoint FromPoint(SpatialPoint point, double distance, ProjectionMode mode)
        {
            return new ProjectedXYZPoint
            {
                X = point.X,
                Y = point.Y,
                Z = point.Z,
                ProjectionDistance = distance,
                Mode = mode
            };
        }

        public static ProjectedXYZPoint FromComponents(IEnumerable<double> components)
        {
            if (components.Count() != 3)
                throw new Exception("Component count must be equal to three.");

            return new ProjectedXYZPoint
            {
                X = components.ElementAt(0),
                Y = components.ElementAt(1),
                Z = components.ElementAt(2)
            };
        }

          #endregion
        #region Properties

        public double ProjectionDistance { get; private set; }

        public double Length
        {
            get { return GeometryExpert.Euclidean(Components); }
            set { Components = GeometryExpert.ScaleToEuclidean(Components, value); }
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public IEnumerable<double> Components
        {
            get
            {
                yield return X;
                yield return Y;
                yield return Z;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public SpatialPoint Source { get; set; }
        public ProjectionMode Mode { get; private set; }


        //public double Dot(SpacialPoint p)
        //{
        //    return GeometryExpert.Dot(this, p);
        //}

        //public SpacialPoint Add(SpacialPoint p)
        //{
        //    return GeometryExpert.Add(this, p);
        //}

        //public SpacialPoint Subtract(SpacialPoint p)
        //{
        //    return GeometryExpert.Subtract(this, p);
        //}

        //public SpacialPoint Multiply(double f)
        //{
        //    return GeometryExpert.Multiply(this, f);
        //}

        public double DistanceFrom(PlanePoint p)
        {
            return GeometryExpert.Euclidean(Components, p.Components);
        }

        //public ProjectedSpacialPoint Project(SpacialPlane plane)
        //{
        //    throw new NotImplementedException();
        //}

        public SpatialPoint Normalize()
        {
            return Multiply(1.0/ Length);
        }

        public SpatialPoint Add(SpatialPoint p)
        {
            return FromComponents(GeometryExpert.AddComponents(p.Components, Components));
        }

        public SpatialPoint Subtract(SpatialPoint p)
        {
            return FromComponents(GeometryExpert.SubtractComponents(p.Components, Components));
        }

        public SpatialPoint Multiply(double f)
        {
            return FromComponents(GeometryExpert.MultiplyComponents(Components, f));
        }

        public double Dot(SpatialPoint p)
        {
            throw new NotImplementedException();
        }

        public double DistanceFrom(SpatialPoint p)
        {
            return GeometryExpert.Euclidean(Components, p.Components);
        }

        public SpatialPoint Cross(SpatialPoint p)
        {
            return GeometryExpert.CrossProduct(this, p);
        }

        public bool IsOrthogonalToApprox(PointComponents p)
        {
            throw new NotImplementedException();
        }

        public string ToShortString()
        {
            return "(" + StringExpert.CommaSeparate(Components) + ")";
        }



        #endregion
    }
}
