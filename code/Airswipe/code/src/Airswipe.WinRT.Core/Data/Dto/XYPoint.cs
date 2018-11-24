using Airswipe.WinRT.Core.Data.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
//using Windows.Foundation;

namespace Airswipe.WinRT.Core.Data
{
    public class XYPoint : PointBase<XYPoint, PlanePoint>, PlanePoint
    {
        #region Constructor

        public XYPoint() { }

        public XYPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        //public static XYPoint FromComponents(IEnumerable<double> components)
        //{
        //    if (components.Count() != 2)
        //        throw new Exception("Component count must be equal to two.");

        //    return new XYPoint(components.ElementAt(0), components.ElementAt(1));
        //}

        public static XYPoint FromPoint(Point p)
        {
            return new XYPoint { X = p.X, Y = p.Y };
        }

        #endregion
        #region Methods


        #endregion
        #region Properties

        public double X { get; set; }
        public double Y { get; set; }

        [JsonIgnore]
        public override IEnumerable<double> Components
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

        #endregion
    }
}
