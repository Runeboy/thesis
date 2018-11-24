using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.Data.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public class OffscreenPoint : SpatialPoint
    {
        public OffscreenPoint()
        {
            Delta = new XYZPoint();
        }

        #region Factory methods

        public static OffscreenPoint FromComponents(IEnumerable<double> components)
        {
            if (components.Count() != 3)
                throw new Exception("Component count must be equal to three.");

            return new OffscreenPoint {
                X = components.ElementAt(0),
                Y = components.ElementAt(1),
                Z = components.ElementAt(2)
            };
        }

        #endregion
        #region Methods

        public string ToShortString()
        {
            return "(" + StringExpert.CommaSeparate(Components) + ")";
        }



        public ProjectedSpatialPoint Project(SpatialPlane plane)
        {
            return GeometryExpert.ProjectOntoPlane(this, plane);
        }

        public double DistanceFrom(SpatialPoint p)
        {
            return GeometryExpert.Euclidean(Components, p.Components);
            //return GeometryExpert.Euclidean(X - p.X, Y - p.Y, Z - p.Z);
        }

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

        public SpatialPoint Subtract(SpatialPoint p)
        {
            return FromComponents(GeometryExpert.SubtractComponents(p.Components, Components));
        }

        public SpatialPoint Multiply(double f)
        {
            return FromComponents(GeometryExpert.MultiplyComponents(Components, f));
        }

        public override string ToString()
        {
            return StringExpert.ToJson(this);
        }

        public double Dot(SpatialPoint p)
        {
            return GeometryExpert.DotComponents(Components, p.Components);
        }

        public SpatialPoint Cross(SpatialPoint p)
        {
            return GeometryExpert.CrossProduct(this, p);
        }

        public SpatialPoint Normalize()
        {
            return Multiply(1.0 / Length);            
        }

        public SpatialPoint Add(SpatialPoint p)
        {
            return FromComponents(GeometryExpert.AddComponents(p.Components, Components));
        }

        public bool IsOrthogonalToApprox(PointComponents p)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region Properties

        public SpatialPoint SphereCenter { get; set; }

        public SpatialPoint Delta { get; set; }

        public SpatialPoint Direction { get; set; }

        public DateTime CaptureTime { get; set; }

        public PointTrackingConfidence Confidence { get; set; }

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

        public double Length
        {
            get { return GeometryExpert.Euclidean(Components); }
            set { Components  = GeometryExpert.ScaleToEuclidean(Components, value); }
        }

        #endregion
        #region Operator overloads

        public static OffscreenPoint operator +(OffscreenPoint p1, OffscreenPoint p2)
        {
            p1.X += p2.X;
            p1.Y += p2.Y;
            p1.Z += p2.Z;

            return p1;
        }

        public static OffscreenPoint operator -(OffscreenPoint p1, OffscreenPoint p2)
        {
            p1.X -= p2.X;
            p1.Y -= p2.Y;
            p1.Z -= p2.Z;

            return p1;
        }

        public static OffscreenPoint operator /(OffscreenPoint p, double denominator)
        {
            p.X /= denominator;
            p.Y /= denominator;
            p.Z /= denominator;

            return p;
        }


        #endregion
    }
}
