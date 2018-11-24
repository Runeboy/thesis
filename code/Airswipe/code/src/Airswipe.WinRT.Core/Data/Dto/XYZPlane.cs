using Airswipe.WinRT.Core.Data.Dto;
using Newtonsoft.Json;
using System;

namespace Airswipe.WinRT.Core.Data
{
    public class XYZPlane : PlaneBase<SpatialPoint>, SpatialPlane
    {
        #region Fields

        //private SpacialPoint normalUnitVector;


        #endregion
        #region Constructors

        //// json deserialization
        //public XYZPlane(XYZPoint normalVector) {
        //    NormalVector = normalVector;
        //}

        public XYZPlane() { }

        public XYZPlane(SpatialPoint _normalVector, double offset) {
            NormalVector = _normalVector;
            Offset = offset;
        }

        public XYZPlane(double a, double b, double c, double d) :this(new XYZPoint(a, c, b), d) // of the form  ax + by + cz = d
        {
        }

        #endregion
        #region Properties

        public double Offset { get; set; }

        //public XYZPlane ReducedDimension
        //{
        //    get
        //    {

        //    }
        //}

        //public SpacialPoint PlanePoint { get; set; }

        [JsonIgnore]
        public SpatialPoint NormalUnitVector
        {
            get { return GeometryExpert.Normalize(NormalVector); } //return normalUnitVector; }
            //set
            //{
            //    if (value.Length != 1)
            //        throw new Exception("Normal vector was not unit length.");

            //    normalUnitVector = value;
            //}
        }

        //private SpatialPoint normalVector { get; set; }
        //public SpatialPoint NormalVector {
        //    get { return normalVector; }
        //    set { normalVector = value.Normalize(); } 
        //}
        public SpatialPoint NormalVector
        {
            get; set;
        }

            #endregion
            #region Factory methods

            //public static XYZPlane Create()
            //{
            //    //TODO create from points
            //    throw new NotImplementedException();
            //}

            //public static Plane Create(Point a, Point b)
            //{
            //    //xxx

            //    return new Plane();

            //}

            #endregion

            //public bool ContainsPoint(SpacialPoint p)
            //{
            //    SpacialPoint maybeInPlaneVector = p.Subtract(PlanePoint);

            //    bool isVectorInPlane = (NormalUnitVector.Dot(maybeInPlaneVector) == 0);

            //    return isVectorInPlane;
            //}

            [JsonIgnore]
        public SpatialPoint Intersects {
            get
            {
                return new XYZPoint(
                    Offset / NormalVector.X, // because  x = d/a   when b=0 and c=0
                    Offset / NormalVector.Y,
                    Offset / NormalVector.Z
                    );
            }
        }

        [JsonIgnore]
        public SpatialPoint FirstIntersect {
            get
            {
                var i = Intersects;

                bool noX = double.IsInfinity(i.X);
                bool noY = double.IsInfinity(i.Y);
                bool noZ = double.IsInfinity(i.Z);

                if (noX && noY && noZ)
                    throw new Exception("Plane intersects no axis??");

                return new XYZPoint(
                    !noX        ? i.X : 0,
                    noX && !noY ? i.Y : 0,
                    noX && noY  ? i.Z : 0 // noZ implicitly false
                    );
            }
        }

        public bool ContainsPoint(SpatialPoint p)
        {
            return ContainsPoint(p, 0);
        }

        public bool ContainsPointApprox(SpatialPoint p)
        {
            return ContainsPoint(p, GeometryExpert.DEFAULT_DOT_PRODUCT_WIGGLE_ROOM);
        }

        public bool ContainsPoint(SpatialPoint p, double grace)
        {
            return Math.Abs( NormalVector.Dot(p) - Offset) <= grace;  // because = ax + by + cy = d  if p=(x,y,z)
        }

        public bool ContainsVector(SpatialPoint p)
        {
            throw new NotImplementedException();
            //return P NormalVector.Dot( ) == Offset;  // because = ax + by + cy = d  if p=(x,y,z)
        }

        [JsonIgnore]
        public string Equation {
            get {
                return string.Format(
                    "{0}x {1}{2}y {3}{4}z = {5}",
                    NormalVector.X,
                    NormalVector.Y < 0 ? "" : "+", 
                    NormalVector.Y,
                    NormalVector.Z < 0? "" : "+",
                    NormalVector.Z,
                    Offset
                    );
            } }

        public override string ToString()
        {
            return StringExpert.ToJson(this);
        }



    }
}
