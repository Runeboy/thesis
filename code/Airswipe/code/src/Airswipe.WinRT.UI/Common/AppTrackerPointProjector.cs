using System;
using Airswipe.WinRT.Core;
using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.Data.Dto;
using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.MotionTracking;

namespace Airswipe.WinRT.UI.Common
{
    public class AppTrackerPointProjector
    {
        private static ILogger log = new TypeLogger<AppTrackerPointProjector>();

        public static readonly AppTrackerPointProjector Instance = new AppTrackerPointProjector();
        //private ProjectedXYPoint lastProjectedPoint;

        public delegate void ProjectedTrackedPointReadyHandler(ProjectedXYPoint projection);

        public event ProjectedTrackedPointReadyHandler TrackingPointProjected;
        public event ProjectedTrackedPointReadyHandler PlaneNormalTrackingPointProjected;
        public event ProjectedTrackedPointReadyHandler DirectionalTrackingPointProjected;
        public event ProjectedTrackedPointReadyHandler SphericalTrackingPointProjected;

        private AppTrackerPointProjector()
        {
            ListenForTrackerPointsAndNotifyProjections();
        }

        private void ListenForTrackerPointsAndNotifyProjections()
        {
            AppMotionTrackerClient.Instance.TrackedPointReady += (OffscreenPoint[] t) =>
            {
                if (t.Length == 0)
                    return;

                OffscreenPoint point = t[t.Length - 1]; // TODO: possibly use last if OptiTrack is the tracker?

                if (AppSettings.InputSpace == null)
                    return;

                var planeNormalProjectedPoint = NotifyProjectionAlongPlaneNormal(point);

                NotifyProjectionAlongDirection(point, planeNormalProjectedPoint);

                NotifyProjectionOnSphere(point, planeNormalProjectedPoint);


                //log.Verbose("Tracking offscreen marker: ({0}:{1}), distance: {2}", displayPosition.X, displayPosition.Y, displayPosition.ProjectionDistance);

                //SetTrackingCursorPosition(displayPosition);



                //SpatialPlane screenPlane = AppSettings.InputSpace.Offscreen;
                //ProjectedSpacialPoint screenProjection = point.Project(screenPlane);

                //InputSpace.
            };
        }


        private void NotifyProjectionOnSphere(OffscreenPoint point, ProjectedXYPoint planeNormalProjectedPoint)
        {

            ////TODO: this assumes the sphere boundary touches the center of the display
            //SpatialPoint sphereCenter = AppSettings.InputSpace.Offscreen.Origo.Add(
            //    fromSphereCenterToScreenCenter.Multiply(-1)
            //    );
            //TODO: this assumes the sphere boundary touches the center of the display
            SpatialPoint sphereCenter = point.SphereCenter;
            double sphereRadius = AppSettings.InputSpace.Offscreen.Origo.Subtract(sphereCenter).Length;

            SpatialPoint fromSphereCenterToScreenCenter = AppSettings.InputSpace.Offscreen.NormalUnitVector.Multiply(
                -sphereRadius
                );

            SpatialPoint fromSphereCenterToPoint = point.Subtract(sphereCenter);

            double fromSphereCenterToPointDistanceFrationOfSphereRadius = (fromSphereCenterToPoint.Length / sphereRadius);// AppSettings.SphereOffscreenRadius);

            SpatialPoint projectionOntoSphere = fromSphereCenterToPoint.Multiply(1 / fromSphereCenterToPointDistanceFrationOfSphereRadius);

            double cosAngleWithSphereCenterToScreenCenter = projectionOntoSphere.Normalize().Dot(
                fromSphereCenterToScreenCenter.Normalize()
                );

            double angleInRadians = Math.Acos(cosAngleWithSphereCenterToScreenCenter);

            double spatialDistanceFromCenterToPointProjectionAlongSphere = sphereRadius * angleInRadians;

            var sphereCenterProjectedOntoScreen = AppSettings.InputSpace.ProjectOffscreenToOnscreenSpace(sphereCenter);

            PlanePoint projectionDirectionFromScreenCenter = planeNormalProjectedPoint.Subtract(sphereCenterProjectedOntoScreen).Normalize();
            //var planeProjection = AppSettings.InputSpace.ProjectOffscreenToOnscreenSpace(point);

            double onscreenDistance = AppSettings.InputSpace.OffscreenDistanceToOnscreenDistance(spatialDistanceFromCenterToPointProjectionAlongSphere);



            PlanePoint projectedPlanePointLocation = sphereCenterProjectedOntoScreen.Add(
                projectionDirectionFromScreenCenter.Multiply(onscreenDistance) // spatialDistanceFromCenterToPointProjectionAlongSphere);
                );
            PlanePoint planeDelta = (LastSphericalProjectedPoint == null ?
                new XYPoint() :
                projectedPlanePointLocation.Subtract(LastSphericalProjectedPoint)
                );

            var distanceFromPointToSphereBoundaryMillimeters = AppSettings.OffscreenDistanceToMillimeter(
                sphereRadius - fromSphereCenterToPoint.Length
                );

            ProjectedXYPoint projectedPlanePoint = new ProjectedXYPoint
            {
                Components = projectedPlanePointLocation.Components,
                ProjectionDistance = distanceFromPointToSphereBoundaryMillimeters,
                Mode = ProjectionMode.Spherical,
                Delta = planeDelta,
                Source = point
            };

            LastSphericalProjectedPoint = projectedPlanePoint;

            if (TrackingPointProjected != null)
                TrackingPointProjected(projectedPlanePoint);

            if (SphericalTrackingPointProjected != null) {
                SphericalTrackingPointProjected(projectedPlanePoint);
            }


            //AppSettings.InputSpace.Offscreen.Origo
        }

        private void NotifyProjectionAlongDirection(OffscreenPoint point, ProjectedXYPoint planeNormalProjectedPoint)
        {
            ProjectedXYPoint projectedPlanePoint = AppSettings.InputSpace.ProjectOffscreenToOnscreenSpace(point, projectionDirection: point.Direction);

            projectedPlanePoint.X += AppSettings.DirectionalOffsetX * projectedPlanePoint.ProjectionDistance ;
            projectedPlanePoint.Y += AppSettings.DirectionalOffsetY * projectedPlanePoint.ProjectionDistance;

            var fromPlaneNormalProjectionToDirectionalProjectionPoint = projectedPlanePoint.Subtract(planeNormalProjectedPoint);

            //projectedPlanePoint.Components = projectedPlanePoint.Add(
            //    fromPlaneNormalProjectionToDirectionalProjectionPoint.Multiply(AppSettings.DirectionalAmplification)
            //    ).Components;
            projectedPlanePoint.Components = planeNormalProjectedPoint.Add(
                fromPlaneNormalProjectionToDirectionalProjectionPoint.Multiply(AppSettings.DirectionalAmplification)
                ).Components;

            PlanePoint planeDelta = (LastDirectionalProjectedPoint == null ?
                new XYPoint() :
                projectedPlanePoint.Subtract(LastDirectionalProjectedPoint)
                );
            projectedPlanePoint.Delta = planeDelta;

            projectedPlanePoint.ProjectionDistance = AppSettings.OnscreenDistanceToMillimeter(projectedPlanePoint.ProjectionDistance);

            LastDirectionalProjectedPoint = projectedPlanePoint;

            if (TrackingPointProjected != null)
                TrackingPointProjected(projectedPlanePoint);

            if (DirectionalTrackingPointProjected != null)
                DirectionalTrackingPointProjected(projectedPlanePoint);
        }

        private ProjectedXYPoint NotifyProjectionAlongPlaneNormal(OffscreenPoint point)
        {
            ProjectedXYPoint projectedPlanePoint = AppSettings.InputSpace.ProjectOffscreenToOnscreenSpace(point);

            PlanePoint projectionPlaneDelta = (LastProjectedPoint == null ?
                new XYPoint() :
                projectedPlanePoint.Subtract(LastProjectedPoint)
                );
            projectedPlanePoint.Delta = projectionPlaneDelta;
            projectedPlanePoint.ProjectionDistance = AppSettings.OnscreenDistanceToMillimeter(projectedPlanePoint.ProjectionDistance);

            LastProjectedPoint = projectedPlanePoint;

            if (TrackingPointProjected != null)
                TrackingPointProjected(projectedPlanePoint);

            if (PlaneNormalTrackingPointProjected != null)
                PlaneNormalTrackingPointProjected(projectedPlanePoint);

            return projectedPlanePoint;
        }

        //private void NotifyPointProjected(ProjectedXYPoint projectedPlanePoint)
        //{
        //}

        //public static AppTrackerPointProjector Instance
        //{
        //    get
        //    {
        //        if (Instance == null)
        //            instance = new AppTrackerPointProjector();

        //        return Instance;
        //    }
        //}

        private ProjectedXYPoint LastProjectedPoint { get; set; }

        public ProjectedXYPoint LastDirectionalProjectedPoint { get; private set; }

        public ProjectedXYPoint LastSphericalProjectedPoint { get; private set; }

        //{
        //    get { return lastProjectedPoint; }
        //    private set { lastProjectedPoint = value; }
        //}
    }
}
