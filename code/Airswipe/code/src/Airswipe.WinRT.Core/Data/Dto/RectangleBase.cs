using System;
using System.Collections.Generic;

namespace Airswipe.WinRT.Core.Data.Dto
{
    public abstract class RectangleBase<TPoint> : PlaneBase<TPoint>, Rectangle<TPoint> where TPoint : PointLocation<TPoint>
    {
        private TPoint origoToRight, origoToBottom; //, toClosestEdge;

        public TPoint Origo { get; set; }

        public TPoint OrigoToRight
        {
            get { return origoToRight; }
            set
            {
                origoToRight = value;
                ErrorIfVectorsNotOthogonal(Orthogonals);
            }
        }

        public TPoint OrigoToBottom
        {
            get { return origoToBottom; }
            set
            {
                origoToBottom = value;
                ErrorIfVectorsNotOthogonal(Orthogonals);
            }
        }

        public double SideRatio
        {
            get { return OrigoToRight.Length / OrigoToBottom.Length; }
            set { GeometryExpert.ChangeRectangleSideRatio(this, value); }
        }

        protected IEnumerable<TPoint> Orthogonals
        {
            get { return new TPoint[] { OrigoToRight, OrigoToBottom }; }
        }

        public double Width
        {
            get { return OrigoToRight.Length * 2; }
        }

        public TPoint GetJoint(RectJointBoundary b)
        {
            return Origo.Add(
                b == RectJointBoundary.BottomLeft || b == RectJointBoundary.TopLeft ? OrigoToRight.Multiply(-1) : OrigoToRight
                ).Add(
                    b == RectJointBoundary.TopLeft || b == RectJointBoundary.TopRight ? OrigoToBottom.Multiply(-1) : OrigoToBottom
                    );
        }

    }
}
