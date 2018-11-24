using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.Data.Dto;
using Newtonsoft.Json;
using System;
using Windows.Foundation;
//using Windows.Foundation;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public class InputSpace
    {
        //public InputSpace() { }

        public InputSpace(Rect onscreen, XYZRect offscreen)
        {
            var onscreenSideRatio = onscreen.Width / onscreen.Height;
            if (Math.Abs(onscreenSideRatio - offscreen.SideRatio) > 1e-15)
                throw new Exception("Side ratios between on- and offscreen dimensions do not match.");

            Onscreen = onscreen;
            Offscreen = offscreen;

            OffScreenPlaneUnitX = offscreen.OrigoToRight.Normalize();
            OffScreenPlaneUnitY = offscreen.OrigoToBottom.Normalize();

            OffscreenToOnscreenRectScale = onscreen.Width / offscreen.Width;
        }

        public ProjectedXYPoint ProjectOffscreenToOnscreenSpace(SpatialPoint p, SpatialPoint projectionDirection = null)
        {
            ProjectedXYPoint pointInOffscreenPlane = GeometryExpert.ProjectSpatialPointAsPlanarOntoRect(p, Offscreen, OffScreenPlaneUnitX, OffScreenPlaneUnitY, projectionDirection: projectionDirection);

            var positionOnDisplay = new XYPoint
            {
                X = Onscreen.Left + Onscreen.Width / 2,
                Y = Onscreen.Top + Onscreen.Height / 2,
            }.Add(
                pointInOffscreenPlane.Multiply(OffscreenToOnscreenRectScale)
                );

            //return new ProjectedXYPoint {
            //    Components = positionOnDisplay.Components,
            //    ProjectionDistance = pointInOffscreenPlane.ProjectionDistance * OffscreenToOnscreenRectScale,
            //};
            pointInOffscreenPlane.Components = positionOnDisplay.Components;
            pointInOffscreenPlane.ProjectionDistance = pointInOffscreenPlane.ProjectionDistance * OffscreenToOnscreenRectScale;

            return pointInOffscreenPlane;
        }



        [JsonIgnore]
        public XYPoint OnscreenRectOrigo
        {
            get
            {
                return new XYPoint
                {
                    X = Onscreen.Left + Onscreen.Width / 2.0,
                    Y = Onscreen.Top + Onscreen.Height / 2.0,
                };
            }
        }

        public double OffscreenDistanceToOnscreenDistance(double d)
        {
            return d * OffscreenToOnscreenRectScale;
        }

        public double OnscreenDistanceToOffscreenDistance(double d)
        {
            return d / OffscreenToOnscreenRectScale;
        }

        #region Properties

        public Rect Onscreen { get; private set; }

        public XYZRect Offscreen { get; private set; }

        private SpatialPoint OffScreenPlaneUnitX { get; set; }
        private SpatialPoint OffScreenPlaneUnitY { get; set; }

        private double OffscreenToOnscreenRectScale { get; set; }


        #endregion


    }
}
