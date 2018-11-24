using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Math = System.Math;

namespace Airswipe.WinRT.Core.Data.Dto
{
    public class XYZPoint : SpatialPoint
    {
        #region Constructor

        public XYZPoint() : this(0, 0, 0) { }

        public XYZPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static XYZPoint FromComponents(IEnumerable<double> components)
        {
            if (components.Count() != 3)
                throw new Exception("Component count must be equal to three.");

            return new XYZPoint(components.ElementAt(0), components.ElementAt(1), components.ElementAt(2));
        }

        //public static XYZPoint NormalizedToUnitLength(double x, double y, double z)
        //{
        //    var point = new XYZPoint(x, y, z);

        //    return point / point.Length;
        //}

        #endregion
        #region Methods

        public XYZPoint RotatePerpendicular(bool x = false, bool y = false, bool z = false)
        {
            double deg = GeometryExpert.DegreeToRadian(90);

            return Rotate(x ? deg : 0, y ? deg : 0, z ? deg : 0);
        }

        public XYZPoint Clone()
        {
            return new XYZPoint(X, Y, Z);
        }

        public XYZPoint RotateDegree(double x = 0, double y = 0, double z = 0) // params are angles in radians
        {
            return Rotate(GeometryExpert.DegreeToRadian(x), GeometryExpert.DegreeToRadian(y), GeometryExpert.DegreeToRadian(z));
        }

        public XYZPoint Rotate(double x, double y, double z) // params are angles in radians
        {
            // rotate z
            var rotated = new XYZPoint(
                X * Math.Cos(z) - Y * Math.Sin(z),
                X * Math.Sin(z) + Y * Math.Cos(z),
                Z
                );

            // rotate x
            rotated = new XYZPoint(
                rotated.X,
                rotated.Y * Math.Cos(x) - rotated.Z * Math.Sin(x),
                rotated.Y * Math.Sin(x) + rotated.Z * Math.Cos(x)
                );

            // rotate y
            rotated = new XYZPoint(
                rotated.X * Math.Cos(y) + rotated.Z * Math.Sin(y),
                rotated.Y,
                -rotated.X * Math.Sin(y) + rotated.Z * Math.Cos(y)
                );

            return rotated;
        }

        public ProjectedSpatialPoint Project(SpatialPlane plane)
        {
            return GeometryExpert.ProjectOntoPlane(this, plane);
        }

        public double Dot(SpatialPoint p)
        {
            return GeometryExpert.Dot(this, p);
        }

        public SpatialPoint Add(SpatialPoint p)
        {
            return FromComponents(GeometryExpert.AddComponents(Components, p.Components));
        }

        public SpatialPoint Subtract(SpatialPoint p)
        {
            return FromComponents(GeometryExpert.SubtractComponents(p.Components, Components));
        }

        public SpatialPoint Multiply(double f)
        {
            return FromComponents(GeometryExpert.MultiplyComponents(Components, f));
        }

        //public XYZPoint Divide(SpacialPoint p)
        //{
        //    return this / p;
        //}

        public SpatialPoint Normalize()
        {
            return Multiply(1.0 / Length);

            //return this / Length;
        }

        public double DistanceFrom(SpatialPoint p)
        {
            //return GeometryExpert.Euclidean(X - p.X, Y - p.Y, Z - p.Z);
            return GeometryExpert.Euclidean(Components, p.Components);
        }

        public override string ToString()
        {
            return StringExpert.ToJson(this);
        }

        public SpatialPoint Cross(SpatialPoint p)
        {
            return GeometryExpert.CrossProduct(this, p);
        }

        public bool IsOrthogonalToApprox(PointComponents p)
        {
            return GeometryExpert.AreComponentsOrthogonalApprox(Components, p.Components);
        }

        public string ToShortString()
        {
            return "(" + StringExpert.CommaSeparate(Components) + ")";
        }

        #endregion
        #region Properties

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public double Length
        {
            get { return GeometryExpert.Euclidean(Components); }
            set { Components = GeometryExpert.ScaleToEuclidean(Components, value); }
        }

        [JsonIgnore]
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
                X = value.ElementAt(0);
                Y = value.ElementAt(1);
                Z = value.ElementAt(2);
            }
        }

        #endregion
        #region Operator overloads

        public static SpatialPoint operator +(XYZPoint p1, SpatialPoint p2)
        {
            return new XYZPoint
            {
                X = p1.X + p2.X,
                Y = p1.Y + p2.Y,
                Z = p1.Z + p2.Z
            };
        }

        public static XYZPoint operator /(XYZPoint p, double denominator)
        {
            return new XYZPoint
            {
                X = p.X / denominator,
                Y = p.Y / denominator,
                Z = p.Z / denominator
            };
        }


        //public static bool operator ==(XYZPoint p1, XYZPoint p2)
        //{
        //    return p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z;
        //}

        //public static bool operator !=(XYZPoint p1, XYZPoint p2)
        //{
        //    return !(p1 == p2);
        //}

        #endregion
            
    }
}
