using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Airswipe.WinRT.Core.Data
{
    // Describes a rectangle in space.
    public class XYZRect : XYZPlane, SpatialRectangle
    {
        private SpatialPoint origo, origoToRight, origoToBottom; //, toClosestEdge;

        [JsonIgnore]
        public SpatialPoint this[RectJointBoundary b]
        {
            get
            {
                switch (b)
                {
                    case RectJointBoundary.TopLeft: return Origo.Subtract(OrigoToBottom).Subtract(OrigoToRight);
                    case RectJointBoundary.TopRight: return Origo.Subtract(OrigoToBottom).Add(OrigoToRight);
                    case RectJointBoundary.BottomLeft: return Origo.Add(OrigoToBottom).Subtract(OrigoToRight);
                    case RectJointBoundary.BottomRight: return Origo.Add(OrigoToBottom).Add(OrigoToRight);
                }
                throw new Exception("Case not handled");
            }
        }

        public SpatialPoint OrigoToRight
        {
            get { return origoToRight; }
            set
            {
                //if (NormalVector.Dot(value) > GeometryExpert.DEFAULT_DOT_PRODUCT_WIGGLE_ROOM)
                //    throw new Exception("Point is not orthogonal to plane normal vector.");

                //if (Origo != null && !ContainsPointApprox(Origo.Add(value))) // -e13
                //    throw new Exception("Plane does not contain point.");

                origoToRight = value;
                ErrorIfVectorsNotOthogonal(Orthogonals);
            }
        }

        public SpatialPoint OrigoToBottom
        {
            get { return origoToBottom; }
            set
            {
                //if (NormalVector.Dot(value) > GeometryExpert.DEFAULT_DOT_PRODUCT_WIGGLE_ROOM)
                //    throw new Exception("Point is not orthogonal to plane normal vector.");

                //if (Origo != null && !ContainsPointApprox(Origo.Add(value))) // -e13
                //    throw new Exception("Plane does not contain point.");

                origoToBottom = value;
                ErrorIfVectorsNotOthogonal(Orthogonals);
            }
        }

        //[JsonConverter(typeof(InterfaceConverter<SpatialPoint, XYZPoint>))]
        public SpatialPoint Origo
        {
            get { return origo; }
            set
            {
                if (NormalVector != null && !ContainsPointApprox(value))
                    throw new Exception("Plane does not contain point.");

                origo = value;
            }
        }

        [JsonIgnore]
        public double SideRatio
        {
            get { return OrigoToRight.Length / OrigoToBottom.Length; }
            set { GeometryExpert.ChangeRectangleSideRatio(this, value); }
        }


        public new SpatialPoint NormalVector
        {
            get { return base.NormalVector; }
            set
            {
                base.NormalVector = value;
                ErrorIfVectorsNotOthogonal(Orthogonals);
            }
        }

        [JsonIgnore]
        public IEnumerable<SpatialPoint> Orthogonals { get { return new List<SpatialPoint> { OrigoToRight, OrigoToBottom, NormalVector }; } }

        [JsonIgnore]
        public double Width
        {
            get { return OrigoToRight.Length * 2; }
        }

        public SpatialPoint GetJoint(RectJointBoundary b)
        {
            return Origo.Add(
                b == RectJointBoundary.BottomLeft || b == RectJointBoundary.TopLeft ? OrigoToRight.Multiply(-1) : OrigoToRight
                ).Add(
                    b == RectJointBoundary.TopLeft || b == RectJointBoundary.TopRight ? OrigoToBottom.Multiply(-1) : OrigoToBottom
                    );
        }

        //public PlaneProjection<PlanePoint> DistanceFromOrigo(SpatialPoint p)
        //{
        //    throw new NotImplementedException();
        //}

        //public SpatialPoint OrigoToRight
        //{
        //    get { return toClosestEdge; }
        //    set
        //    {
        //        if (NormalVector.Dot(value) != 0)
        //            throw new Exception("Vector point to closest edge is not orthogonal to plane normal vector.");

        //        var location = Origo.Add(value);
        //        if (!ContainsPointApprox(location)) // -e13
        //            throw new Exception("Plane does not contain vector point to closest edge.");

        //        toClosestEdge = value;
        //    }
        //}

        //public SpatialPoint ToClosestEdge
        //{
        //    get { return toClosestEdge; }
        //    set
        //    {
        //        if (NormalVector.Dot(value) != 0)
        //            throw new Exception("Vector point to closest edge is not orthogonal to plane normal vector.");

        //        var location = Origo.Add(value);
        //        if (!ContainsPointApprox(location)) // -e13
        //            throw new Exception("Plane does not contain vector point to closest edge.");

        //        toClosestEdge = value;
        //    }
        //}

        //public double SideRatio { get; set; }
    }
}
