using Newtonsoft.Json;
using System.Collections.Generic;

namespace Airswipe.WinRT.Core.Data.Dto
{
    public abstract class PointBase<TPoint, TAbstraction> where TAbstraction : PointComponents where TPoint : TAbstraction, new()
    {
        #region Constructor

        public TAbstraction FromComponents(IEnumerable<double> components)
        {
            //if (components.Count() != 2)
            //    throw new Exception("Component count must be equal to two.");

            //return new XYPoint(components.ElementAt(0), components.ElementAt(1));

            return new TPoint() { Components = components };
        }

        #endregion
        #region Methods

        public double DistanceFrom(TAbstraction p)
        {
            return GeometryExpert.Euclidean(Components, p.Components);
        }

        public TAbstraction Add(TAbstraction p)
        {
            return FromComponents(GeometryExpert.AddComponents(p.Components, Components));
        }

        public TAbstraction Subtract(TAbstraction p)
        {
            return FromComponents(GeometryExpert.SubtractComponents(p.Components, Components));
        }

        public TAbstraction Multiply(double f)
        {
            return FromComponents(GeometryExpert.MultiplyComponents(Components, f));
        }

        public override string ToString()
        {
            return StringExpert.ToJson(this);
        }

        public double Dot(PlanePoint p)
        {
            return GeometryExpert.DotComponents(Components, p.Components);
        }

        public TAbstraction Normalize()
        {
            return Multiply(1.0 / Length);
        }

        public bool IsOrthogonalToApprox(PointComponents p)
        {
            return GeometryExpert.AreComponentsOrthogonalApprox(Components, p.Components);
        }

        #endregion
        #region Properties

        //public double X { get; set; }
        //public double Y { get; set; }

        [JsonIgnore]
        public abstract IEnumerable<double> Components { get; set; }
        //{
        //    get
        //    {
        //        yield return X;
        //        yield return Y;
        //    }
        //    set
        //    {
        //        X = value.ElementAt(0);
        //        Y = value.ElementAt(1);
        //    }
        //}

        [JsonIgnore]
        public double Length
        {
            get { return GeometryExpert.Euclidean(Components); }
            set { Components = GeometryExpert.ScaleToEuclidean(Components, value); }
        }

        #endregion

    }
}
