using Microsoft.VisualStudio.TestTools.UnitTesting;
using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.Data.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Statistics.Analysis;
using Accord.Math.Decompositions;

namespace Airswipe.WinRT.Core.Test
{
    [TestClass]
    public class GeometryExpertTest : GeometryExpert
    {
        [TestMethod]
        public void TestProjectOntoRectIn45DegreeWithBaseHorisontalLengthInXYPlane()
        {
            var rect = new XYZRect
            {
                NormalVector = new XYZPoint(1, 1, 1),
                Offset = 3,
                Origo = new XYZPoint(1, 1, 1),
                OrigoToBottom = new XYZPoint(0.75, 0.75, -1.5),
                OrigoToRight = new XYZPoint(-1, 1, 0),
            };

            var projectionOntoOrigo = ProjectOntoRect(
                new XYZPoint(2, 2, 2),
                rect
                );
            Assert.IsTrue(Math.Sqrt(3) - Math.Abs(projectionOntoOrigo.ProjectionDistance) < 1e-15);
            Assert.AreEqual(0, projectionOntoOrigo.X);
            Assert.AreEqual(0, projectionOntoOrigo.Y);

            var projectionNorthEast = ProjectOntoRect(
                rect.Origo.Add(new XYZPoint(0.5, 1, 1.5)),
                rect
                );
            Assert.IsTrue(projectionNorthEast.ProjectionDistance > 0);
            Assert.IsTrue(projectionNorthEast.X > 0);
            Assert.IsTrue(projectionNorthEast.Y < 0);

            var projectionNorth = ProjectOntoRect(
                rect.Origo.Add(new XYZPoint(0, 0, 0.1)),
                rect
                );
            Assert.IsTrue(projectionNorth.ProjectionDistance > 0);
            Assert.IsTrue(projectionNorth.X == 0);
            Assert.IsTrue(projectionNorth.Y < 0);

            var projectionEast = ProjectOntoRect(
                rect.Origo.Add(rect.OrigoToRight.Normalize()).Add(rect.NormalUnitVector),
                rect
                );
            Assert.IsTrue(projectionEast.ProjectionDistance > 0);
            Assert.IsTrue(projectionEast.X > 0);
            Assert.IsTrue(Math.Abs(projectionEast.Y) < DEFAULT_DOT_PRODUCT_WIGGLE_ROOM); // == 0);

            var projectionWest = ProjectOntoRect(
                rect.Origo.Subtract(rect.OrigoToRight.Normalize()).Add(rect.NormalUnitVector),
                rect
                );
            Assert.IsTrue(projectionWest.ProjectionDistance > 0);
            Assert.IsTrue(projectionWest.X < 0);
            Assert.IsTrue(Math.Abs(projectionWest.Y) < DEFAULT_DOT_PRODUCT_WIGGLE_ROOM);

            var projectionBottomEdge = ProjectOntoRect(
                rect.Origo.Add(rect.OrigoToBottom),
                rect
                );
            Assert.IsTrue(projectionBottomEdge.ProjectionDistance == 0);
            Assert.IsTrue(projectionBottomEdge.X == 0);
            Assert.IsTrue(Math.Abs(projectionBottomEdge.Y - rect.OrigoToBottom.Length) < 1e-15);

            var projectionSouthEastBehind = ProjectOntoRect(
                new XYZPoint(0.1, 2.9, -0.1),
                rect
                );
            Assert.IsTrue(projectionSouthEastBehind.ProjectionDistance < 0);
            Assert.IsTrue(projectionSouthEastBehind.X > 0);
            Assert.IsTrue(projectionSouthEastBehind.Y > 0);
        }

        [TestMethod]
        public void TestVectorLengthScaling()
        {
            var v = new XYPoint(3, 4);

            const int newLength = 10;
            Assert.AreNotEqual(v.Length, newLength);

            v.Length = newLength;

            Assert.AreEqual(v.Length, newLength);
        }

        [TestMethod]
        public void TestChangeSquareToRectangle()
        {
            int length = 3;
            var rect = new XYRect
            {
                Origo = new XYPoint(0, 0),
                OrigoToRight = new XYPoint(length, 0),
                OrigoToBottom = new XYPoint(0, length)
            };
            Assert.AreEqual(length, rect.OrigoToRight.Length);
            Assert.AreEqual(length, rect.OrigoToBottom.Length);

            double newRatio = 4 / 3.0;

            ChangeRectangleSideRatio(rect, newRatio);

            Assert.AreNotEqual(length, rect.OrigoToRight.Length);
            Assert.AreNotEqual(length, rect.OrigoToBottom.Length);

            Assert.AreEqual(newRatio, rect.SideRatio);
            //Assert.AreEqual(Math.Pow(length,2), newLengths.X * newLengths.Y);
        }

        [TestMethod]
        public void TestFitSpatialPointsToPlane()
        {
            //var points = new SpatialPoint[] {  // runs along x axis tilted 45 degrees out towards viewer
            //    new XYZPoint(1, 2, 0.9), // top-left
            //    new XYZPoint(3, 2, 1.1), // top-right
            //    new XYZPoint(3, 1, 1.9), // bottom-right
            //    new XYZPoint(1, 1, 2.1), // bottom-left
            //};

            //var plane = FitToPlane(points);

            var plane = new XYZPlane
            {
                NormalVector = new XYZPoint(1, 1, 1),
                Offset = 3
            };

            var linePoints = new RectLinePoints<SpatialPoint>
            {
                Top = new List<SpatialPoint> {
                    new XYZPoint(1, 2, 1),
                    new XYZPoint(3, 2, 1),
                    },
                Right = new List<SpatialPoint> {
                    new XYZPoint(3, 2, 1),
                    new XYZPoint(3, 1, 1),
                    },
                Bottom = new List<SpatialPoint> {
                    new XYZPoint(3, 1, 1),
                    new XYZPoint(1, 1, 1),
                    },
                Left = new List<SpatialPoint> {
                    new XYZPoint(1, 1, 1),
                    new XYZPoint(1, 2, 1),
                    }
            };
            //var linePoints = new RectLinePoints<SpatialPoint>
            //{
            //    Top = new List<SpatialPoint> {
            //        new XYZPoint(1.12, 2.05, 0.98),
            //        new XYZPoint(3.13, 2.01, 0.97),
            //        },
            //    Right = new List<SpatialPoint> {
            //        new XYZPoint(3.13, 2.01, 0.97),
            //        //new XYZPoint(2.96, 1.99, 1.92),
            //        new XYZPoint(3.02, 0.96, 1.04),
            //        },
            //    Bottom = new List<SpatialPoint> {
            //        new XYZPoint(3.02, 0.96, 1.04),
            //        //new XYZPoint(3.01, 0.97, 1.01),
            //        new XYZPoint(1.09, 0.98, 1.12),
            //        },
            //    Left = new List<SpatialPoint> {
            //        new XYZPoint(1.09, 0.98, 1.12),
            //        //new XYZPoint(1, 1, 1),
            //        new XYZPoint(1.12, 2.05, 0.98),
            //        }
            //};

            RectLinePoints<ProjectedSpatialPoint> planeProjectedPoints = linePoints.Apply(p => ProjectOntoPlane(p, plane));

            //planeProjectedPoints.To

            var expectedDecreasingProjectionDistanceOrdering = new List<RectLineBoundary> { RectLineBoundary.Right, RectLineBoundary.Bottom, RectLineBoundary.Top, RectLineBoundary.Left };
            expectedDecreasingProjectionDistanceOrdering.Aggregate((b1, b2) =>
            {
                Assert.IsTrue(planeProjectedPoints[b1].First().ProjectionDistance >= planeProjectedPoints[b2].First().ProjectionDistance);
                return b2;
            });

            //{
            //    Top = lines.Top.Select(p => ProjectOntoPlane(p, plane)),
            //    Right = lines.Right.Select(p => ProjectOntoPlane(p, plane)),
            //    Bottom = lines.Bottom.Select(p => ProjectOntoPlane(p, plane)),
            //    Left = lines.Left.Select(p => ProjectOntoPlane(p, plane))
            //};


            Rectangle<SpatialPoint> fit = GetSpatialRectangleFromSpatialLinePoints(plane, planeProjectedPoints);

            //const double expectedRatio = ..............;
            var sideRatio = fit.OrigoToRight.Length / fit.OrigoToBottom.Length;


            // TODO: verify by more than visual inspection
            //throw new NotImplementedException();
        }

        [TestMethod]
        public void TestPointsToPlane()
        {
            var lines = RectLinePoints<PlanePoint>.Init<XYPoint>(
                top: new double[,] { { 1, 6 }, { 1.2, 6.1 }, { 0.5, 7.1 }, { 1, 8 }, { 2, 9 }, { 3, 9.3 } },
                right: new double[,] { { 3, 9.1 }, { 4, 9 }, { 5, 8 }, { 5.5, 7.5 }, { 6, 7 }, { 7, 6 } },
                bottom: new double[,] { { 6.3, 5.2 }, { 6, 5 }, { 4, 4.6 }, { 3.5, 2.7 }, { 2.4, 2.6 }, { 2, 1 } },
                left: new double[,] { { 1.1, 1 }, { 0, 2.1 }, { -1.8, 3 }, { -3, 5 } }
                );

            //double[,] top =  {
            //        { 1,6 }, { 1.2, 6.1 }, {0.5, 7.1}, { 1, 8 }, { 2,9 }, {3,9.3}
            //    };
            //    double[,] right =  {
            //        { 3, 9.1 }, { 4,9 }, {  5,8 }, { 5.5, 7.5  }, { 6,7 }, {  7,6}
            //    };
            //    double[,] bottom =  {
            //        { 6.3, 5.2 }, { 6,5 }, { 4,4.6 }, { 3.5, 2.7  }, { 2.4, 2.6 }    , { 2, 1}
            //    };
            //    double[,] left =  {
            //        { 1.1,1 }, { 0,2.1 }, { -1.8, 3 }, { -3, 5  }
            //    };
            Func<double[,], IEnumerable<PlanePoint>> toPlanePoints = (l) => Enumerable.Range(0, l.GetLength(0)).Select(rowIndex => new XYPoint(l[rowIndex, 0], l[rowIndex, 1])).Cast<PlanePoint>();

            FitPlanarRectangle(lines);
            //FitPlanarRectangle(toPlanePoints(top), toPlanePoints(right), toPlanePoints(bottom), toPlanePoints(left));
        }



        [TestMethod]
        public void TestFittingConstantZPlane()
        {
            var points = new SpatialPoint[] {
                new XYZPoint(1, 1, 0.9),
                new XYZPoint(2, 1, 1.1),
                new XYZPoint(2, 2, 0.9),
                new XYZPoint(1, 2, 1.1),
            };

            var plane = GetPlaneFromPoints(points);

            Assert.AreEqual(0, Math.Round(plane.NormalUnitVector.X));
            Assert.AreEqual(0, Math.Round(plane.NormalUnitVector.Y));
            Assert.AreEqual(1, plane.NormalUnitVector.Z);
        }

        [TestMethod]
        public void TestFit45DegreePlaneAlongX()
        {
            var points = new SpatialPoint[] {
                new XYZPoint(1, 1, 0.9),
                new XYZPoint(2, 1, 1.1),
                new XYZPoint(1, -1, 2.1),
                new XYZPoint(2, -1, 1.9),
            };

            var plane = GetPlaneFromPoints(points);

            Assert.IsTrue(plane.NormalVector.Y == 0.5 * plane.NormalVector.Z);

            Assert.AreEqual(0, Math.Round(plane.NormalUnitVector.X));
            Assert.AreEqual(0, Math.Round(plane.NormalUnitVector.Y));
        }

        [TestMethod]
        public void TestDotProduct()
        {
            var a = new XYZPoint(2, 3, 4);
            var b = new XYZPoint(5, 6, 7);

            double c = a.Dot(b); //CrossProduct(a, b);

            Assert.AreEqual(c,
                a.X * b.X +
                a.Y * b.Y +
                a.Z * b.Z);
        }

        [TestMethod]
        public void TestCrossProduct()
        {
            var a = new XYZPoint(2, 3, 4);
            var b = new XYZPoint(5, 6, 7);

            var c = a.Cross(b); //CrossProduct(a, b);

            Assert.IsTrue(c.X == -3);
            Assert.IsTrue(c.Y == 6);
            Assert.IsTrue(c.Z == -3);
        }

        [TestMethod]
        public void TestCrossProduct2()
        {
            var a = new XYZPoint(1, 0, 0);
            var b = new XYZPoint(0, 1, 0);

            var c = a.Cross(b); //CrossProduct(a, b);

            Assert.IsTrue(c.X == 0);
            Assert.IsTrue(c.Y == 0);
            Assert.IsTrue(c.Z == 1);
        }

        [TestMethod]
        public void TestCrossProduct5()
        {
            var a = new XYZPoint(1, 0, 0);
            var b = new XYZPoint(1, 0.5, 0);

            var c = a.Cross(b); //CrossProduct(a, b);

            Assert.IsTrue(c.X == 0);
            Assert.IsTrue(c.Y == 0);

            //double expectedZ = Math.Sin(Math.PI / 4.0) * a.Length * b.Length;
            Assert.AreEqual(0.5, c.Z);
        }

        [TestMethod]
        public void TestCrossProduct6()
        {
            var a = new XYZPoint(3, -3, 1);
            var b = new XYZPoint(4, 9, 2);

            var c = a.Cross(b); //CrossProduct(a, b);

            Assert.AreEqual(-15, c.X);
            Assert.AreEqual(-2, c.Y);
            Assert.AreEqual(39, c.Z);
        }

        [TestMethod]
        public void TestCrossProduct3()
        {
            var a = new XYZPoint(0, 1, 0);
            var b = new XYZPoint(0, 0, 1);

            var c = a.Cross(b); //CrossProduct(a, b);

            Assert.IsTrue(c.X == 1);
            Assert.IsTrue(c.Y == 0);
            Assert.IsTrue(c.Z == 0);
        }

        [TestMethod]
        public void TestCrossProduct4()
        {
            var a = new XYZPoint(0, 0, 1);
            var b = new XYZPoint(1, 0, 0);

            var c = a.Cross(b); //CrossProduct(a, b);

            Assert.IsTrue(c.X == 0);
            Assert.IsTrue(c.Y == 1);
            Assert.IsTrue(c.Z == 0);
        }

        //[TestMethod]
        //public void TestFittingConstantZPlane22222222222222()
        //{
        //    //var expectedPlane = new XYZPlane
        //    //{
        //    //};

        //    var normalVector = new XYZPoint(0, 0, 1);
        //    //var planeVector = new XYZPoint(1, 1, 0);

        //    var planeVector = normalVector.RotatePerpendicular(x:true, y: true, z:true);

        //    var skjod = normalVector.Dot(planeVector);
        //    Assert.AreEqual(0, Math.Round(normalVector.Dot(planeVector), 10));


        //    ////Assert.IsTrue(0 == normalVector.Dot(planeVector));

        //    //const double wiggle = 0.1;
        //    ////for (double d = -normalVector.Length; d <= normalVector.Length; d += normalVector.Length / 10.0)
        //    //for (double d = 10; d <= 360; d += 10.0)
        //    //{
        //    //    var r = DegreeToRadian(d);
        //    //    var skod = CrossProductSolveB(planeVector, normalVector, r);  //planeVector.RotateDegree(y: d);
        //    //    //var skod2 = planeVector.RotatePerpendicular(x: true);

        //    //}

        //    var plane = FitToPlane(new SpacialPoint[] {
        //        new XYZPoint(1, -1, 1),
        //        new XYZPoint(-1 ,1 -1),
        //        new XYZPoint(, 78, 40),
        //        new XYZPoint(25000, 40, 124),
        //        new XYZPoint(18000, 85, 68),
        //        new XYZPoint(19000, 80, 90)
        //    });

        //    //Assert.IsTrue(plane.NormalUnitVector.X == expected.X);
        //    //Assert.IsTrue(plane.NormalUnitVector.Y == expected.Y);
        //    //Assert.IsTrue(plane.NormalUnitVector.Z == expected.Z);
        //}

        [TestMethod]
        public void TestPlaneCenterPointInPlane() // should project a point onto a horisontal plane
        {
            var plane = new XYZPlane
            {
                NormalVector = new XYZPoint(1, 1, 1),
                //PlanePoint = new XYZPoint(2, 2, 2)
                Offset = 6
            };

            var point = new XYZPoint(2, 2, 2);

            Assert.IsTrue(plane.ContainsPoint(point));
        }

        [TestMethod]
        public void TestPointsIn45DegreePlane() // should project a point onto a horisontal plane
        {
            // using plane x+y+z = 3, 45 degree decrease on all axis from (1,1,1)
            var plane = new XYZPlane
            {
                NormalVector = new XYZPoint(1, 1, 1),
                //PlanePoint = new XYZPoint(1, 1, 1)
                Offset = 3
            };

            Assert.IsTrue(plane.ContainsPoint(new XYZPoint(3, 0, 0)));
            Assert.IsTrue(plane.ContainsPoint(new XYZPoint(0, 3, 0)));
            Assert.IsTrue(plane.ContainsPoint(new XYZPoint(0, 0, 3)));
            Assert.IsTrue(plane.ContainsPoint(new XYZPoint(1, 1, 1)));

            Assert.IsTrue(plane.ContainsPoint(new XYZPoint(1, 2, 0)));
            Assert.IsTrue(plane.ContainsPoint(new XYZPoint(0, 1, 2)));
            Assert.IsTrue(plane.ContainsPoint(new XYZPoint(1, 0, 2)));
            Assert.IsTrue(plane.ContainsPoint(new XYZPoint(2, 1, 0)));
            Assert.IsTrue(plane.ContainsPoint(new XYZPoint(2, 0, 1)));
        }

        [TestMethod]
        public void TestPointNotInPlane() // should project a point onto a horisontal plane
        {
            var plane = new XYZPlane
            {
                NormalVector = new XYZPoint(1, 1, 1),
                //PlanePoint = new XYZPoint(2, 2, 2)
                Offset = 99
            };

            var point = new XYZPoint(2, 3, 2);

            Assert.IsFalse(plane.ContainsPoint(point));
        }

        [TestMethod]
        public void TestHorizontalPointProjection() // should project a point onto a horisontal plane
        {
            var plane = new XYZPlane
            {
                NormalVector = new XYZPoint(0, 1, 0),
                //PlanePoint = new XYZPoint(0, 3, 0)
                Offset = 3
            };

            var pointToProject = new XYZPoint(10, 20, -5);

            ProjectedSpatialPoint projectionPoint = ProjectOntoPlane(pointToProject, plane);

            Assert.IsTrue(plane.ContainsPoint(projectionPoint));

            Assert.IsTrue(projectionPoint.X == pointToProject.X);
            Assert.IsTrue(projectionPoint.Y == 3);
            Assert.IsTrue(projectionPoint.Z == pointToProject.Z);


            Assert.AreEqual(pointToProject.Y - 3, projectionPoint.ProjectionDistance);
        }

        [TestMethod]
        public void Test45DegreePointProjection()
        {
            var plane = new XYZPlane
            {
                NormalVector = new XYZPoint(1, 1, 1),
                //PlanePoint = new XYZPoint(1, 1, 1)
                Offset = 3
            };

            var pointToProject = new XYZPoint(3, 0, 0).Add(plane.NormalUnitVector);

            SpatialPoint projected = ProjectOntoPlane(pointToProject, plane);

            Assert.IsTrue(plane.ContainsPoint(projected));

            Assert.AreEqual(3, projected.X);
            Assert.AreEqual(0, Math.Round(projected.Y, 10));
            Assert.AreEqual(0, Math.Round(projected.Z));
        }

        [TestMethod]
        public void TestPCA()
        {
            ////double[,] sourceMatrixT =
            ////{
            ////    { 2.5,  2.4 },
            ////    { 0.5,  0.7 },
            ////    { 2.2,  2.9 },
            ////    { 1.9,  2.2 },
            ////    { 3.1,  3.0 },
            ////    { 2.3,  2.7 },
            ////    { 2.0,  1.6 },
            ////    { 1.0,  1.1 },
            ////    { 1.5,  1.6 },
            ////    { 1.1,  0.9 }
            ////};


            //double[,] sourceMatrixT =
            //{
            //    { 1.0,  2.3 },
            //    { 2.3,  1 - 0.5 },
            //    { 3.0,  2.0 },
            //    { 2.5,  4.0 + 0.5 },
            //};

            ////double[,] sourceMatrixT = // 
            ////{
            ////    { 1.0,  2.3 },
            ////    { 2.0 - 3,  1 - 0.5 },
            ////    { 3.0,  2.0 },
            ////    { 2.0 + 3,  3.0 + 0.5 },
            ////};

            //double[][] sourceMatrix = new Double[sourceMatrixT.Length / 2][];
            //for (int i = 0; i < sourceMatrixT.Length / 2; i++)
            //{
            //    sourceMatrix[i] = new Double[2];

            //    //sourceMatrix[i][0] = sourceMatrixT[i, 0];
            //    //sourceMatrix[i][1] = sourceMatrixT[i, 1];
            //    sourceMatrix[i][0] = sourceMatrixT[i, 0];
            //    sourceMatrix[i][1] = sourceMatrixT[i, 1];// * -1 + 3;
            //}
            ////double[,] sourceMatrix =
            ////{
            ////    { 2.5,  2.4 },
            ////    { 0.5,  0.7 },
            ////    { 2.2,  2.9 },
            ////    { 1.9,  2.2 },
            ////    { 3.1,  3.0 },
            ////    { 2.3,  2.7 },
            ////    { 2.0,  1.6 },
            ////    { 1.0,  1.1 },
            ////    { 1.5,  1.6 },
            ////    { 1.1,  0.9 }
            ////};

            //var pca = new MyPCA(sourceMatrix);

            //////  Creates the Principal Component Analysis of the given source
            ////var pca = new PrincipalComponentAnalysis(sourceMatrix, AnalysisMethod.Center);

            ////// Compute the Principal Component Analysis
            ////pca.Compute();

            ////var c1 = pca.Components[0];
            ////var c2 = pca.Components[1];


            ////TD
            //var transformed = pca.Transform(sourceMatrix);//, 0.8f, true);

            ////var comp1 = e1.Eigenvector[0];
            ////var comp2 = e1.Eigenvector[1];

            ////bool mainCompIsHorizontal = Math.Abs(c1.Eigenvector[0] / c1.Eigenvector[1]) > 1;
            ////var transformed2 = pca.Transform(sourceMatrix).Select(
            ////    p => mainCompIsHorizontal?
            ////        new XYPoint(p[0], p[1]) :
            ////        new XYPoint(p[1], p[0]) 
            ////        );
            //var transformed2 = pca.Transform(sourceMatrix).Select(
            //    p => new XYPoint(p[0], p[1])
            //    );

            ////if (!mainCompIsHorizontal)
            ////    transformed2 = transformed2.Select(p => new XYPoint(p.Y, p.X));


            //string before = StringExpert.CommaSeparate(sourceMatrix.Select((da, i) => "(" + da[0] + "," + da[1] + ")/P" + i));
            //string after = StringExpert.CommaSeparate(transformed2.Select((da, i) => "(" + Math.Round(da.X, 2) + "," + Math.Round(da.Y, 2) + ")/P" + i));
            ////string before = StringExpert.CommaSeparate(sourceMatrix.Select(da => "{" + da[0] + "," + da[1] + "}"));
            ////string after = StringExpert.CommaSeparate(transformed2.Select(da => "{" + Math.Round(da.X, 2) + "," + Math.Round(da.Y, 2) + "}"));

            ////var skod = pca.Transform(new double[,] { { -1, .5 } });

            /////////// principal component analysis
            ////var x = c1.Eigenvector[0];
            ////var y = c1.Eigenvector[1];
            ////var angleRadians = Math.Acos(x); // simple because the vector has unit (hypotenuse) length
            ////var angleDegrees = GeometryExpert.RadianToDegree(angleRadians); // simple because the vector has unit (hypotenuse) length
            ////////////

            //var mean = new XYPoint(pca.Mean.X, pca.Mean.Y);

            ////var plotE1 = string.Format("({0},{1}) -- ({2},{3})",
            ////    mean.X, mean.Y,
            ////    mean.X + pca.Eig1VectorUnit.X,
            ////    mean.Y + pca.Eig1Vector.Y);

            ////var plotE2 = string.Format("({0},{1}) -- ({2},{3})",
            ////    mean.X, mean.Y,
            ////    mean.X + pca.Eig2Vector.X,
            ////    mean.Y + pca.Eig2Vector.Y);

            ////var s = StringExpert.ToJson(sourceMatrix);
            ////var ss = StringExpert.FromJson<double[][]>(s);
            //////pca.
            //// Creates a projection considering 80% of the information
        }

        [TestMethod]
        public void TestPCA2()
        {
            string s = "[  {    \"X\": 513.99455566406255,    \"Y\": 705.25715332031245  },  {    \"X\": -8.3923767089843757,    \"Y\": 906.333984375  },  {    \"X\": -931.469873046875,    \"Y\": 80.510296630859372  },  {    \"X\": -639.95134277343755,    \"Y\": 921.6222045898437  },  {    \"X\": 997.4691162109375,    \"Y\": 532.91238403320312  },  {    \"X\": -800.80330810546877,    \"Y\": 541.36506347656245  },  {    \"X\": 1154.0234619140624,    \"Y\": 29.630824279785156  },  {    \"X\": -368.36167602539064,    \"Y\": -751.06672363281245  },  {    \"X\": 822.04434814453123,    \"Y\": -523.85650634765625  },  {    \"X\": 506.45557861328126,    \"Y\": -663.51937255859377  },  {    \"X\": -755.8697448730469,    \"Y\": -414.86227416992187  },  {    \"X\": 111.92345581054687,    \"Y\": -789.01181640625  }]";
            var data = StringExpert.FromJson<List<XYPoint>>(s);

            //            double[,] dataT =
            //            {
            //    { 2.5,  2.4 },
            //    { 0.5,  0.7 },
            //    { 2.2,  2.9 },
            //    { 1.9,  2.2 },
            //    { 3.1,  3.0 },
            //    { 2.3,  2.7 },
            //    { 2.0,  1.6 },
            //    { 1.0,  1.1 },
            //    { 1.5,  1.6 },
            //    { 1.1,  0.9 }
            //};
            //            var pca = new MyPCA(MyPCA.PointsToDouble(dataT));


            var pca = new MyPCA(MyPCA.PointsToDouble(data));

            var l1 = Math.Sqrt(pca.Eig1VectorUnit.Eigenvalue);
            var l2 = Math.Sqrt(pca.Eig2VectorUnit.Eigenvalue);




            double[] mean = new double[] { pca.Mean.X, pca.Mean.Y };

            // Step 3. Compute the covariance matrix
            // -------------------------------------

            double[,] covariance = Accord.Statistics.Measures.Covariance(MyPCA.PointsToDouble(data), mean);

            var evd = new EigenvalueDecomposition(covariance);


            // Create the analysis using the covariance matrix
            //var pca = PrincipalComponentAnalysis.FromCovarianceMatrix(mean, covariance);

            // Compute it
            //pca.Compute();


            // Step 4. Compute the eigenvectors and eigenvalues of the covariance matrix
            //--------------------------------------------------------------------------

            // Those are the expected eigenvalues, in descending order:
            //double[] eigenvalues = { 1.28402771, 0.0490833989 };

            // And this will be their proportion:
            //double[] proportion = eigenvalues.Divide(eigenvalues.Sum());
        }


        [TestMethod]
        public void Skod() {

            //var mapper = StringExpert.FromJson<Exp3PointMapper>("{  \"PCA\": {    \"IsXInverted\": true,    \"SkewRatio\": 0.742213392356757,    \"Mean\": {      \"X\": 177.59127277798123,      \"Y\": 529.35047361585828    },    \"Eig1VectorUnit\": {      \"Eigenvalue\": 9662145.987989312,      \"X\": -0.835482717757317,      \"Y\": 0.54951672251974959    },    \"Eig2VectorUnit\": {      \"Eigenvalue\": 7171374.1511917766,      \"X\": -0.54951672251974959,      \"Y\": -0.835482717757317    },    \"PrincipalAngle\": 2.5598069668561574,    \"PrincipalAngleDegree\": 146.66613556904241  }}");

            //var toMap =  mapper.PCA.Mean;//new XYPoint(0, -2000);

            var mapper = StringExpert.FromJson<Exp3PointMapper>("{  \"PCA\": {    \"IsXInverted\": true,    \"SkewRatio\": 0.73233363551820507,    \"Mean\": {      \"X\": 108.68019809722901,      \"Y\": 322.33007300694783    },    \"Eig1VectorUnit\": {      \"Eigenvalue\": 4642996.7584800767,      \"X\": -0.90349918621313086,      \"Y\": 0.42858980448933959    },    \"Eig2VectorUnit\": {      \"Eigenvalue\": 3400222.6958369561,      \"X\": -0.42858980448933959,      \"Y\": -0.90349918621313086    },    \"PrincipalAngle\": 2.6986612710039029,    \"PrincipalAngleDegree\": 154.62190116393413  }}");

            var actual = new XYPoint(-3000, 3000);
            //var actual = mapper.PCA.Mean;
            var predicted = mapper.FromModeledToNormalSpace(
                mapper.FromNormalToModeledSpace(actual)
                );
            //var p2 = mapper.GetTransformedPoint(from);
            //var detasn = mapper.Detransform(from);
        }

    }

}
