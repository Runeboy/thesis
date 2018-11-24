using Airswipe.WinRT.Core.Data;
using System;
//using Windows.Foundation;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Airswipe.WinRT.Core.MotionTracking
{
    /// <summary>
    /// Represent a point on a two-dimensional display.
    /// </summary>
    public class OnscreenPoint : PlanePoint
    {

        public static OnscreenPoint FromComponents(IEnumerable<double> components)
        {
            if (components.Count() != 2)
                throw new Exception("Component count must be equal to two.");

            return new OnscreenPoint { X = components.ElementAt(0), Y = components.ElementAt(1) };
        }

        #region Properties 

        public DateTime CaptureTime { get; set; }

        public IEnumerable<double> Components
        {
            get
            {
                yield return X;
                yield return Y;
            }
            set
            {
                X = value.ElementAt(0);
                Y = value.ElementAt(1);
            }
        }

        public double Length
        {
            get { return GeometryExpert.Euclidean(Components); }
            set { Components = GeometryExpert.ScaleToEuclidean(Components, value); }
        }

        public double X { get; set; }
        public double Y { get; set; }

        #endregion 
        #region Factory methods

        public static OnscreenPoint FromPoint(Point p)
        {
            return new OnscreenPoint
            {
                X = p.X,
                Y = p.Y,
                CaptureTime = DateTime.Now
            };
        }

        public double DistanceFrom(PlanePoint p)
        {
            return GeometryExpert.Euclidean(X - p.X, Y - p.Y);                
        }

        public PlanePoint Multiply(double f)
        {
            throw new NotImplementedException();
        }

        public PlanePoint Add(PlanePoint p)
        {
            return FromComponents(GeometryExpert.AddComponents(p.Components, Components));
        }

        public PlanePoint Subtract(PlanePoint p)
        {
            return FromComponents(GeometryExpert.SubtractComponents(p.Components, Components));
        }

        public override string ToString()
        {
            return StringExpert.ToJson(this);
        }

        public double Dot(PlanePoint p)
        {
            return GeometryExpert.DotComponents(Components, p.Components);
        }

        public PlanePoint Normalize()
        {
            return Multiply(1.0 / Length);
        }

        public bool IsOrthogonalToApprox(PointComponents p)
        {
            return GeometryExpert.AreComponentsOrthogonalApprox(Components, p.Components);
        }

        #endregion
    }
}
