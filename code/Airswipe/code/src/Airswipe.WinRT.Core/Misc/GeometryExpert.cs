using Matrix = Accord.Math.Matrix;
using Airswipe.WinRT.Core.Data.Dto;
using Airswipe.WinRT.Core.Fitting;
using Airswipe.WinRT.Core.Log;
//using Microsoft.SolverFoundation.Common;
//using Microsoft.SolverFoundation.Solvers;
using System;
using System.Collections.Generic;
using System.Linq;
using Airswipe.WinRT.Core.Misc;
using Airswipe.WinRT.Core.MotionTracking;
using Windows.Foundation;

namespace Airswipe.WinRT.Core.Data
{
    public class GeometryExpert
    {
        private static ILogger log = new TypeLogger<GeometryExpert>();

        public const double DEFAULT_DOT_PRODUCT_WIGGLE_ROOM = 1e-14;

        public static void ChangeRectangleSideRatio<T>(Rectangle<T> rect, double newRatio) where T : PointComponents
        {
            log.Verbose("Rescaling rectangle from {0} to {1}..", rect.SideRatio, newRatio);

            var currentLowerRightRectArea = rect.OrigoToRight.Length * rect.OrigoToBottom.Length;

            rect.OrigoToRight.Length = Math.Sqrt(currentLowerRightRectArea * newRatio);
            rect.OrigoToBottom.Length = Math.Sqrt(currentLowerRightRectArea / newRatio);
        }

        //public PlanePoint ChangeRectangleSideRatio(double currentHorisontalSideLength, double currentVerticalSideLength, double newRatio)
        //{
        //    return new XYPoint
        //    {
        //        X = Math.Sqrt(currentHorisontalSideLength * currentVerticalSideLength * newRatio),
        //        Y = Math.Sqrt(currentHorisontalSideLength * currentVerticalSideLength / newRatio)
        //    };
        //}


        public static XYZRect GetSpatialRectangleFromSpatialLinePoints(
            SpatialPlane plane,
            RectLinePoints<ProjectedSpatialPoint> planeProjectedPoints,
            double horisontalOverVerticalRatio = 1
            )
        {
            foreach (var p in planeProjectedPoints.Concatenated) // just to make sure, should be unnecessary
                if (!plane.ContainsPointApprox(p))
                    throw new Exception("One or more points does not lie in the plane given.");

            SpatialPoint projectionSpatialOrigo = planeProjectedPoints.Concatenated.Cast<SpatialPoint>().Aggregate(
                (p1, p2) => p1.Add(p2)).Multiply(1.0 / planeProjectedPoints.Concatenated.Count()
                );
            if (!plane.ContainsPointApprox(projectionSpatialOrigo))
                throw new Exception("Plane does not contain the origo (calculated as the mean of the projected points).");

            var unitX = new XYZPoint { X = 1 };
            //var unitY = new XYZPoint { Y = 1 };

            SpatialPoint newUnitY = unitX.Cross(plane.NormalVector).Normalize();
            SpatialPoint newUnitX = plane.NormalVector.Cross(newUnitY).Normalize();
            if (newUnitX.Dot(newUnitY) > DEFAULT_DOT_PRODUCT_WIGGLE_ROOM)
                throw new Exception("Unit vectors not perpendicular");

            RectLinePoints<PlanePoint> planePoints = planeProjectedPoints.Apply(
                p => GetLocationInPlane(p, projectionSpatialOrigo, newUnitX, newUnitY)
                );

            Rectangle<PlanePoint> planeFit = FitPlanarRectangle(planePoints);
            if (horisontalOverVerticalRatio != 1)
                throw new NotImplementedException();

            SpatialPoint planeFitSpatialOrigo = projectionSpatialOrigo.Add(
                newUnitX.Multiply(planeFit.Origo.X)
                ).Add(
                    newUnitY.Multiply(planeFit.Origo.Y)
                    );
            if (!plane.ContainsPointApprox(planeFitSpatialOrigo))
                throw new Exception("Plane does not contain the fitted spatial origo.");

            var origoToEdgeDistances = new {
                Right = newUnitX.Multiply(planeFit.OrigoToRight.X).Add(
                    newUnitY.Multiply(planeFit.OrigoToRight.Y)
                    ),
                Bottom = newUnitX.Multiply(planeFit.OrigoToBottom.X).Add(
                    newUnitY.Multiply(planeFit.OrigoToBottom.Y)
                    )
            };

            return new XYZRect
            {
                NormalVector = plane.NormalVector,
                Offset = plane.Offset,
                Origo = planeFitSpatialOrigo,
                OrigoToRight = origoToEdgeDistances.Right,
                OrigoToBottom = origoToEdgeDistances.Bottom
                //SideRatio = planeFit.SideRatio,
                //ToClosestEdge = fittedSpatialOrigoToClosestEdge
            };
        }

        internal static IEnumerable<double> ScaleToEuclidean(IEnumerable<double> components, double value)
        {
            double ratio = value / Euclidean(components);

            return MultiplyComponents(components, ratio);
        }

        public static bool AreComponentsOrthogonalApprox(IEnumerable<double> c1, IEnumerable<double> c2)
        {
            return DotComponents(c1, c2) <= DEFAULT_DOT_PRODUCT_WIGGLE_ROOM;
        }

        public static PlanePoint GetLocationInPlane(SpatialPoint point, SpatialPoint origo, SpatialPoint xUnit, SpatialPoint yUnit)
        {
            // point must be in  plane spanned by unit vectors, otherwise it does not make sense 
            SpatialPoint originToPoint = point.Subtract(origo);

            return new XYPoint
            {
                X = xUnit.Dot(originToPoint), /// scalar projection of the pointon the new X-axis, because  a DOT b = cos(theta) |b|  when |a|=1  
                Y = yUnit.Dot(originToPoint),
            };
        }

        public static XYRect FitPlanarRectangle(RectLinePoints<PlanePoint> lines)// IEnumerable<PlanePoint> top, IEnumerable<PlanePoint> right, IEnumerable<PlanePoint> bottom, IEnumerable<PlanePoint> left)
        {
            var fit = ConstrainedLeastSquareFit.FitRectangleLines(lines);

            var n1 = fit.n[0];
            var n2 = fit.n[1];

            //var n = new XYPoint(fit.n[0], fit.n[1]);
            //var nR = new XYPoint(-fit.n[1], fit.n[0]);
            //var skod = n.Dot(nR);

            var c1 = fit.c[0];
            var c2 = fit.c[1];
            var c3 = fit.c[2];
            var c4 = fit.c[3];

            //double[] rightSide = Matrix.Multiply(-1, new double[] {
            //    c1, c3, c3, c1 , c2, c2, c4, c4
            //});

            double[] rightSide = new double[] {
                    -c1, -c3, -c3, -c1 , -c2, -c2, -c4, -c4
                 };

            //var c1c2 = Matrix.Solve(
            //    new double[,]
            //    {
            //        {  n1, n2 },
            //        { -n2, n1  }
            //    },
            //    new double[] { -c1, -c2 }
            //    );

            //var c2c3 = Matrix.Solve(
            //    new double[,]
            //    {
            //        { -n2, n1 },
            //        { n1, n2  }
            //    },
            //    new double[] { -c2, -c3 }
            //    );

            //var c3c4 = Matrix.Solve(
            //    new double[,]
            //    {
            //        {  n1, n2  },
            //        { -n2, n1 },
            //    },
            //    new double[] { -c3, -c4 }
            //    );

            //var c4c1 = Matrix.Solve(
            //    new double[,]
            //    {
            //        {  n2, -n1 },
            //        {  n1, n2 },
            //    },
            //    new double[] { -c4, -c1 }
            //    );

            //       c1 + (n1 + n2) 
            //     4 --- 1 
            //  c4 |     | c2 + (-n2, n1)
            //     3 --- 2
            //       c3

            double[,] A = {
                {  n1,   n2,   0,    0,    0,    0,    0,    0     }, // c1
                {  0,    0,    n1,   n2,   0,    0,    0,    0     }, // c3
                {  0,    0,    0,    0,    n1,   n2,   0,    0     }, // c3
                {  0,    0,    0,    0,    0,    0,    n1,   n2   }, // c1
                { -n2,   n1,   0,    0,    0,    0,    0,    0     }, // c2
                {  0,    0 ,  -n2,   n1,   0,    0,    0,    0     }, // c2
                {  0,    0,    0,    0,   -n2,   n1,   0,    0     }, // c4
                {  0,    0,    0,    0,    0,    0,   -n2,   n1,  }, // c4
            };

            //var intersectVector = Matrix.Solve(A, rightSide);
            var intersectVector = Matrix.Multiply( // because A^T = A^-1, since A column space is orthogonal
                Matrix.Transpose(A),
                rightSide
                );

            var intersects = new
            {
                topRight = new XYPoint(intersectVector[0], intersectVector[1]),
                bottomRight = new XYPoint(intersectVector[2], intersectVector[3]), // not strictly necessary
                bottomLeft = new XYPoint(intersectVector[4], intersectVector[5]),
                topLeft = new XYPoint(intersectVector[6], intersectVector[7]),
            };



            // form two orthogonal vectors for the rectangle, from the the top-left corner
            PlanePoint horisontal = intersects.topRight.Subtract(intersects.topLeft);
            PlanePoint vertical = intersects.bottomLeft.Subtract(intersects.topLeft);

            PlanePoint origo = intersects.topLeft.Add(horisontal.Multiply(0.5)).Add(vertical.Multiply(0.5));
            //// input in wolfram confirms these points are in fact perpendiclar

            var texStuff = new
            {
                intersects = PlotExpert.PointsToTexScatterPointCsvData(
                    new XYPoint[] { intersects.topRight, intersects.bottomRight, intersects.bottomLeft, intersects.topLeft, intersects.topRight }.Cast<PointComponents>()
                    ),
                intersectOrigo = PlotExpert.PointsToTexScatterPointCsvData(
                    new PlanePoint[] { origo }.Cast<PointComponents>(),
                    "B"
                    ),
                scatter = PlotExpert.PointsToTexScatterPointCsvData(
                    lines.Concatenated.Cast<PointComponents>(),
                    "C"
                    ),
                scatterOrigo = PlotExpert.TexPointData(
                    "D",
                    lines.Concatenated.Aggregate((p1, p2) => p1.Add(p2)).Multiply(lines.Concatenated.Count())
                    ),
            };

            //bool isHorisontalShortEdge = (horisontal.Length < vertical.Length);

            return new XYRect
            {
                Origo = origo,
                OrigoToRight = horisontal.Multiply(0.5),
                OrigoToBottom = vertical.Multiply(0.5)
                //SideRatio = isHorisontalShortEdge ? horisontal.Length / vertical.Length : vertical.Length / horisontal.Length,
                //ToClosestEdge = isHorisontalShortEdge ? vertical.Multiply(0.5) : horisontal.Multiply(0.5)
            };
        }

        //public static SpacialPoint CrossComponents(IEnumerable<double> c1, IEnumerable<double> c2)
        //{
        //    return new XYZPoint
        //    {
        //        X = Determinant(a.Y, a.Z, b.Y, b.Z),
        //        Y = -Determinant(a.X, a.Z, b.X, b.Z),
        //        Z = Determinant(a.X, a.Y, b.X, b.Y)
        //    };


        //}

        public static double Euclidean(Point p)
        {
            return Euclidean(p.X, p.Y);
        }

        public static double Euclidean(PlanePoint p)
        {
            return Euclidean(p.X, p.Y);
        }

        public static double Euclidean(params double[] axisValues)
        {
            return Math.Sqrt(
                axisValues.Sum(v => Math.Pow(v, 2))
                );
        }

        public static double Euclidean(IEnumerable<double> axisValues)
        {
            return Math.Sqrt(
                axisValues.Sum(v => Math.Pow(v, 2))
                );
        }

        public static double Euclidean(IEnumerable<double> p1Components, IEnumerable<double> p2Components)
        {
            EnsureComponentCountMatch(p1Components, p2Components);

            return Math.Sqrt(
                p1Components.Select((c, i) => c - p2Components.ElementAt(i)).Sum(v => Math.Pow(v, 2))
                );
        }

        private static void EnsureComponentCountMatch(IEnumerable<double> p1Components, IEnumerable<double> p2Components)
        {
            if (p1Components.Count() != p2Components.Count())
                throw new Exception("Component count does not match");
        }

        // fits a plane by solving matrix system Ax = b composed by the gradients of the components of the squared error
        public static XYZPlane GetPlaneFromPoints(IEnumerable<SpatialPoint> points)
        {
            // we can make the matrix cell operations for A explicit by 3x3 matrix 
            Func<SpatialPoint, double>[,] leastSquaresMinimumDiffForA = {
                { (p) => Math.Pow(p.X, 2),  (p) => p.X * p.Y,         (p) => p.X },
                { (p) => p.X * p.Y,         (p) => Math.Pow(p.Y, 2),  (p) => p.Y },
                { (p) => p.X,               (p) => p.Y,               (p) => 1   }
            };

            // ..same for 3-vector for b 
            Func<SpatialPoint, double>[] leastSquaresMinimumDiffForB = {
                (p) => p.X * p.Z,   (p) => p.Y * p.Z,    (p) => p.Z
            };

            const int m = 3; // row count
            const int n = 3; // col count                

            var A = new double[m, n]; // all entries will be zero initially
            var b = new double[m];

            foreach (SpatialPoint point in points)
                for (int i = 0; i < m; i++)
                {
                    b[i] += leastSquaresMinimumDiffForB[i](point);

                    for (int j = 0; j < n; j++)
                        A[i, j] += leastSquaresMinimumDiffForA[i, j](point);
                }

            // MMM
            double[] normalVectorABC = Matrix.Solve(A, b, leastSquares: false); // solves for vector x = [x1,x2,x3]^T
                                                                                //double[] normalVectorABC = MatrixExpert.MatrixProduct(
                                                                                //    MatrixExpert.MatrixInverse(A), b
                                                                                //    ); 

            //MatrixExpert.SystemSolve(A, b); // solves for vector x = [x1,x2,x3]^T

            var planeScalarEqCoefficients = new // convert to scalar equation:  z = ax + by + d  => -ax - by + cz = d   where c = 1 
            {
                a = -normalVectorABC[0],
                b = -normalVectorABC[1],
                c = 1,
                d = normalVectorABC[2]
            };

            return new XYZPlane
            {
                NormalVector = new XYZPoint
                {
                    X = planeScalarEqCoefficients.a,
                    Y = planeScalarEqCoefficients.b,
                    Z = planeScalarEqCoefficients.c
                },
                Offset = planeScalarEqCoefficients.d
                //PlanePoint = new XYZPoint
                //{
                //    X = 0,
                //    Y = 0,
                //    Z = normalVector.c // we were solving for z=ax+by+c, so z=c for x=0, y=y
                //}
            };
        }

        public static double Determinant(double a, double b, double c, double d)
        {
            return a * d - b * c;
        }

        //public static XYZPoint CrossProductSolveB(SpacialPoint a, SpacialPoint n, double angleBetweenAAndB)
        //{
        //    var b = Accord.Math.Matrix.Solve(
        //        new double[,] {
        //            {  0   , -a.Z,   a.Y },
        //            {  a.Z ,  0   , -a.X },
        //            { -a.Y,   a.X ,  0 },
        //        },
        //        new double[] {
        //            a.Length * Math.Sin(angleBetweenAAndB) * n.X,
        //            a.Length * Math.Sin(angleBetweenAAndB) * n.Y,
        //            a.Length * Math.Sin(angleBetweenAAndB) * n.Z,
        //            },
        //        leastSquares:true
        //        );

        //    if (b.Length != 3)
        //        throw new Exception("Unexpected result");

        //    return new XYZPoint(b[0], b[1], b[2]);
        //}

        public static XYZPoint CrossProduct(SpatialPoint a, SpatialPoint b)
        {
            return new XYZPoint
            {
                X = Determinant(a.Y, a.Z, b.Y, b.Z),
                Y = -Determinant(a.X, a.Z, b.X, b.Z),
                Z = Determinant(a.X, a.Y, b.X, b.Y)
            };
        }

        public static double DegreeToRadian(double d)
        {
            return (Math.PI / 180.0) * d;
        }

        public static double RadianToDegree(double r)
        {
            return (180.0 / Math.PI) * r;
        }

        // Returns vector from origo of rect
        public static ProjectedXYPoint ProjectOntoRect(SpatialPoint point, SpatialRectangle rect)
        {
            return ProjectSpatialPointAsPlanarOntoRect(point, rect, rect.OrigoToRight.Normalize(), rect.OrigoToBottom.Normalize());
        }

        // Returns vector from origo of rect
        public static ProjectedXYPoint ProjectSpatialPointAsPlanarOntoRect(SpatialPoint point, SpatialRectangle rect, SpatialPoint unitX, SpatialPoint unitY, SpatialPoint projectionDirection = null)
        {
            ProjectedSpatialPoint pointInPlane = ProjectOntoPlane(point, rect, rect.Origo, direction: projectionDirection);

            // use orientation vectors as unit vectors
            PlanePoint planeLocation = GetLocationInPlane(
                pointInPlane, //point, 
                rect.Origo, 
                unitX, unitY
                );

            return new ProjectedXYPoint // optinally just use z of ProjectedSpatialPoint for distance
            {
                X = planeLocation.X,
                Y = planeLocation.Y,
                ProjectionDistance = pointInPlane.ProjectionDistance,
                Mode = pointInPlane.Mode,
                Source = point
            };
        }

        // Returns projection, i.e. point on plane and the vector that was used to get to the plane.
        public static ProjectedSpatialPoint ProjectOntoPlane(SpatialPoint point, SpatialPlane plane, SpatialPoint anyPlanePoint = null, SpatialPoint direction = null)
        {
            var planePoint = anyPlanePoint?? plane.FirstIntersect;    // far or close plane ref point may decrease numerical precision

            SpatialPoint fromSomewhereInPlaneToProjectionPoint = point.Subtract(planePoint);

            //double perpendicularDistanceToPlane = plane.NormalUnitVector.Dot(fromSomewhereInPlaneToProjectionPoint); // because  a dot b = |a| |b| cos(theta)  and |b| = 1 
            double perpendicularDistanceToPlane = plane.NormalUnitVector.Dot(point) - plane.Offset / plane.NormalVector.Length; // because  a dot b = |a| |b| cos(theta)  and |b| = 1 

            //perpendicularDistanceFromPlane = perpendicularDistanceFromPlane / cosTheta;
            //

            SpatialPoint planeToPointAlongNormalVector = plane.NormalUnitVector.Multiply(perpendicularDistanceToPlane);

            SpatialPoint planeNormalProjectedPoint = point.Subtract(planeToPointAlongNormalVector);

            if (direction != null)
            {
                var directionUnit = direction.Normalize();

                //var pointToPlaneAlongNormal = planeToPointAlongNormalVector.Multiply(-1);

                //var cosTheta = pointToPlaneAlongNormal.Normalize().Dot(directionNormal);
                var cosTheta = plane.NormalUnitVector.Multiply(-1).Dot(directionUnit);

                //log.Verbose(cosTheta.ToString());

                var directionalDistanceToPlane = perpendicularDistanceToPlane / cosTheta; // since |A| cos(theta) = distance along (reversed) normal

                //log.Verbose(directionalDistanceToPlane + " | " + perpendicularDistanceToPlane);

                //log.Verbose(directionLengthToPlane.ToString());

                var directionalProjectedPoint = point.Add(directionUnit.Multiply(directionalDistanceToPlane));

                return ProjectedXYZPoint.FromPoint(directionalProjectedPoint,directionalDistanceToPlane, ProjectionMode.Directional);
            }

            return ProjectedXYZPoint.FromPoint(
                planeNormalProjectedPoint,
                perpendicularDistanceToPlane,
                ProjectionMode.PlaneNormal
                );
        }

        public static XYZPoint Multiply(SpatialPoint p, double f)
        {
            return new XYZPoint
            {
                X = p.X * f,
                Y = p.Y * f,
                Z = p.Z * f
            };
        }

        public static XYZPoint Add(SpatialPoint p1, SpatialPoint p2)
        {
            return new XYZPoint
            {
                X = p1.X + p2.X,
                Y = p1.Y + p2.Y,
                Z = p1.Z + p2.Z
            };
        }

        public static XYZPoint Subtract(SpatialPoint p1, SpatialPoint p2)
        {
            return new XYZPoint
            {
                X = p1.X - p2.X,
                Y = p1.Y - p2.Y,
                Z = p1.Z - p2.Z
            };
        }

        public static IEnumerable<double> AddComponents(IEnumerable<double> c2, IEnumerable<double> c1)
        {
            return c1.Select((c, i) => c + c2.ElementAt(i));
        }

        public static IEnumerable<double> SubtractComponents(IEnumerable<double> subtraction, IEnumerable<double> fromValue)
        {
            return fromValue.Select((c, i) => c - subtraction.ElementAt(i));
        }

        public static IEnumerable<double> MultiplyComponents(IEnumerable<double> c1, IEnumerable<double> c2)
        {
            return c2.Select((c, i) => c * c1.ElementAt(i));
        }

        public static IEnumerable<double> MultiplyComponents(IEnumerable<double> c1, double f)
        {
            return c1.Select((c, i) => c * f);
        }

        public static double DotComponents(IEnumerable<double> c1, IEnumerable<double> c2)
        {
            return c1.Select((c, i) => c * c2.ElementAt(i)).Sum();
        }

        //public static XYZPoint Subtract(Plane p1, SpacialPoint p2)
        //{
        //    return new XYZPoint
        //    {
        //        X = p1.X - p2.X,
        //        Y = p1.Y - p2.Y,
        //        Z = p1.Z - p2.Z
        //    };
        //}

        public static double Dot(SpatialPoint p1, SpatialPoint p2)
        {
            return
                p1.X * p2.X +
                p1.Y * p2.Y +
                p1.Z * p2.Z;
        }

        public static SpatialPoint Normalize(SpatialPoint p)
        {
            return p.Multiply(1.0 / p.Length);
        }


        //public static SpatialPlane Ahhahaha()
        //{
        //    SimplexSolver solver;
        //    solver = new SimplexSolver();

        //    const string Z = "ObjectiveFunction";

        //    // variable id 
        //    int vid;

        //    // row id 
        //    int rid;

        //    string[] stockVarnames = new string[] { "a", "b", "c" };
        //    // Set porfolio allocation bounds  
        //    foreach (string varName in stockVarnames)
        //    {
        //        solver.AddVariable(varName, out vid);
        //        //                solver.SetBounds(vid, 0, 75000);
        //        solver.SetBounds(vid, Rational.NegativeInfinity, Rational.PositiveInfinity);
        //    }

        //    //----------------------------


        //    //// add constraints to model
        //    //for (int i = 0; i < monthNames.Length; i++)
        //    //{
        //    //    // get a row id
        //    //    this.solver.AddRow(monthNames[i], out rid);

        //    //    // start to setup the coefficient for this row 
        //    //    int nameIndex = 0;
        //    //    foreach (double coef in this.stockMonthlyReturn[i])
        //    //    {
        //    //        vid = this.solver.GetIndexFromKey(stockVarnames[nameIndex]);
        //    //        this.solver.SetCoefficient(rid, vid, coef);
        //    //        nameIndex++;
        //    //    }

        //    //    // last column on this row -Z 
        //    //    this.solver.SetCoefficient(rid, this.solver.GetIndexFromKey(Z), -1);

        //    //    // row >= 0
        //    //    this.solver.SetBounds(rid, 0, Rational.PositiveInfinity);
        //    //}

        //    //// add budget constraint  Xa + Xb + Xc + Xd + Xe <= 100000
        //    //this.solver.AddRow("Budget constraint", out rid);
        //    //foreach (String name in stockVarnames)
        //    //{
        //    //    this.solver.SetCoefficient(rid, this.solver.GetIndexFromKey(name), 1);
        //    //}

        //    //this.solver.SetBounds(rid, Rational.NegativeInfinity, 100000);

        //    //// add return demand constraint  0.020Xa + 0.0316Xb + 0.0323Xc + 0.0337Xd + 0.0376Xe >= 3000
        //    //this.solver.AddRow("Return demand constraint", out rid);
        //    //for (int index = 0; index < stockVarnames.Length; index++)
        //    //{
        //    //    this.solver.SetCoefficient(rid, this.solver.GetIndexFromKey(stockVarnames[index]), this.returnDemand[index]);
        //    //}

        //    //this.solver.SetBounds(rid, 3000, Rational.PositiveInfinity);

        //    //// add goal 
        //    //this.solver.AddRow("Goal", out rid);
        //    //this.solver.SetCoefficient(rid, this.solver.GetIndexFromKey(Z), 1);

        //    //// add objective (goal) to model and specify minimization (==false)
        //    //this.solver.AddGoal(rid, 1, false);


        //    //=--------------------------------------------
        //    SimplexSolverParams parameter1 = new SimplexSolverParams();
        //    parameter1.Algorithm = SimplexAlgorithmKind.Primal;
        //    parameter1.Costing = SimplexCosting.SteepestEdge;
        //    parameter1.InitialBasisKind = SimplexBasisKind.Slack;
        //    parameter1.UseExact = true;

        //    SimplexSolverParams parameter2 = new SimplexSolverParams();
        //    parameter2.Algorithm = SimplexAlgorithmKind.Dual;

        //    solver.Solve(parameter1, parameter2);

        //    WriteLinearSolverResults(solver);

        //    throw new NotImplementedException();
        //}



        //private static void WriteLinearSolverResults(SimplexSolver solver)
        //{
        //    //throw new NotImplementedException();

        //    log.Verbose("Writing solver stats...");
        //    log.Verbose(StringExpert.ToJson(new
        //    {
        //        PivotCount = solver.PivotCount
        //        //    Pivot CountDegen= [{0}]", solver.PivotCountDegenerate);
        //        //    Console.WriteLine("Pivot CountExact= [{0}]", solver.PivotCountExact);
        //        //    Console.WriteLine("Pivot CountE P1 = [{0}]", solver.PivotCountExactPhaseOne);
        //        //    Console.WriteLine("Pivot CountE P2 = [{0}]", solver.PivotCountExactPhaseTwo);
        //        //    Console.WriteLine("Pivot Count Dbl = [{0}]", solver.PivotCountDouble);
        //        //    Console.WriteLine("Pivot CountD P1 = [{0}]", solver.PivotCountDoublePhaseOne);
        //        //    Console.WriteLine("Pivot CountD P2 = [{0}]", solver.PivotCountDoublePhaseTwo);
        //        //    Console.WriteLine("Factor Count    = [{0}]", solver.FactorCount);
        //        //    Console.WriteLine("Factor Count Ext= [{0}]", solver.FactorCountExact);
        //        //    Console.WriteLine("Factor Count Dbl= [{0}]", solver.FactorCountDouble);
        //        //    Console.WriteLine("Branch Count    = [{0}]", solver.BranchCount);

        //        //    Console.WriteLine();
        //    }));


        //}

        //public static SpacialPlane Ahhahaha(SpacialPoint p1, SpacialPoint p2, SpacialPoint p3, SpacialPoint p4)
        //{
        //    SimplexSolver solver;
        //    solver = new SimplexSolver();

        //    const String Z = "ObjectiveFunction";

        //    // variable id 
        //    int vid;

        //    // row id 
        //    int rid;

        //    string[] stockVarnames = new string[] { "a", "b", "c" };
        //    // Set porfolio allocation bounds  
        //    foreach (string varName in stockVarnames)
        //    {
        //        solver.AddVariable(varName, out vid);
        //        //                solver.SetBounds(vid, 0, 75000);
        //        solver.SetBounds(vid, Rational.NegativeInfinity, Rational.PositiveInfinity);
        //    }

        //    //----------------------------


        //    //// add constraints to model
        //    //for (int i = 0; i < monthNames.Length; i++)
        //    //{
        //    //    // get a row id
        //    //    this.solver.AddRow(monthNames[i], out rid);

        //    //    // start to setup the coefficient for this row 
        //    //    int nameIndex = 0;
        //    //    foreach (double coef in this.stockMonthlyReturn[i])
        //    //    {
        //    //        vid = this.solver.GetIndexFromKey(stockVarnames[nameIndex]);
        //    //        this.solver.SetCoefficient(rid, vid, coef);
        //    //        nameIndex++;
        //    //    }

        //    //    // last column on this row -Z 
        //    //    this.solver.SetCoefficient(rid, this.solver.GetIndexFromKey(Z), -1);

        //    //    // row >= 0
        //    //    this.solver.SetBounds(rid, 0, Rational.PositiveInfinity);
        //    //}

        //    //// add budget constraint  Xa + Xb + Xc + Xd + Xe <= 100000
        //    //this.solver.AddRow("Budget constraint", out rid);
        //    //foreach (String name in stockVarnames)
        //    //{
        //    //    this.solver.SetCoefficient(rid, this.solver.GetIndexFromKey(name), 1);
        //    //}

        //    //this.solver.SetBounds(rid, Rational.NegativeInfinity, 100000);

        //    //// add return demand constraint  0.020Xa + 0.0316Xb + 0.0323Xc + 0.0337Xd + 0.0376Xe >= 3000
        //    //this.solver.AddRow("Return demand constraint", out rid);
        //    //for (int index = 0; index < stockVarnames.Length; index++)
        //    //{
        //    //    this.solver.SetCoefficient(rid, this.solver.GetIndexFromKey(stockVarnames[index]), this.returnDemand[index]);
        //    //}

        //    //this.solver.SetBounds(rid, 3000, Rational.PositiveInfinity);

        //    //// add goal 
        //    //this.solver.AddRow("Goal", out rid);
        //    //this.solver.SetCoefficient(rid, this.solver.GetIndexFromKey(Z), 1);

        //    //// add objective (goal) to model and specify minimization (==false)
        //    //this.solver.AddGoal(rid, 1, false);


        //    //=--------------------------------------------
        //    SimplexSolverParams parameter1 = new SimplexSolverParams();
        //    parameter1.Algorithm = SimplexAlgorithmKind.Primal;
        //    parameter1.Costing = SimplexCosting.SteepestEdge;
        //    parameter1.InitialBasisKind = SimplexBasisKind.Slack;
        //    parameter1.UseExact = true;

        //    SimplexSolverParams parameter2 = new SimplexSolverParams();
        //    parameter2.Algorithm = SimplexAlgorithmKind.Dual;

        //    solver.Solve(parameter1, parameter2);

        //    WriteLinearSolverResults(solver);

        //    throw new NotImplementedException();
        //}

        //public static SpacialPlane FitSpacialRect(double sideDimension, PlanePoint[] p1, PlanePoint[] p2, PlanePoint[] p3, PlanePoint[] p4)
        //{
        //    var sets = new PlanePoint[][] { p1, p2, p3, p4 };

        //    int pointCount = p1.Length + p2.Length + p3.Length + p4.Length;
        //    int colCount = sets.Length + 2;

        //    //var A = new double[pointCount, colCount];
        //    var A = new double[pointCount, colCount];
        //    var b = new double[pointCount];
        //    int rowIndex = 0;
        //    for (int setIndex = 0; setIndex < sets.Length; setIndex++)
        //    {
        //        var set = sets[setIndex];

        //        bool isSetIndexEqual = (setIndex % 2 == 0);

        //        //var row = new double[colCount];
        //        for (int pointIndex = 0; pointIndex < set.Length; pointIndex++)
        //        {
        //            var point = set[pointIndex];

        //            A[rowIndex, setIndex] = 1;

        //            A[rowIndex, colCount - 2] = isSetIndexEqual ? point.X : point.Y;
        //            A[rowIndex, colCount - 1] = isSetIndexEqual ? point.Y : -point.X;
        //        }

        //        rowIndex++;
        //    }

        //    var skod = Matrix.Solve(A, b, leastSquares: true);

        //    throw new NotImplementedException();            

        //}

    }
}
