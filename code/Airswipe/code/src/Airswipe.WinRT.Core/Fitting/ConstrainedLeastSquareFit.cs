using Airswipe.WinRT.Core.Data;
using System;
using System.Collections.Generic;
using Accord.Math;
using System.Linq;
using Airswipe.WinRT.Core.Data.Dto;

namespace Airswipe.WinRT.Core.Fitting
{
    // Special Linear Least Squares Problems with Quadratic Constraint, implementations of theory from the book
    public class ConstrainedLeastSquareFit
    {
        #region Properties

        public double[] c { get; private set; }
        public double[] n { get; private set; }

        #endregion
        #region Constructors 

        //public ClSq(params PlanePoint[] points) // fits a line
        //: this(points.ToList())
        //{ }

        public static ConstrainedLeastSquareFit FitParallelLines(IEnumerable<PlanePoint> l1, IEnumerable<PlanePoint> l2) // fits parallel lines
        {
            return new ConstrainedLeastSquareFit(
                PointsToArray(l1.ToList(), l2.ToList()),
                2
                );
        }

        public static ConstrainedLeastSquareFit FitOrthogonalLines(IEnumerable<PlanePoint> l1, IEnumerable<PlanePoint> l2) // fits parallel lines
        {
            return new ConstrainedLeastSquareFit(
                PointsToOrthogonalLinesFitArray(l1.ToList(), l2.ToList()),
                2
                );
        }

        public static ConstrainedLeastSquareFit FitRectangleLines(RectLinePoints<PlanePoint> lines)//IEnumerable<PlanePoint> top, IEnumerable<PlanePoint> right, IEnumerable<PlanePoint> bottom, IEnumerable<PlanePoint> left) 
        {
            //return new ClSq(
            //    PointsToRectangularFitArray(
            //        top.ToList(), 
            //        right.ToList(), 
            //        bottom.ToList(), 
            //        left.ToList()
            //        ),
            //    2
            //    );
            return new ConstrainedLeastSquareFit(PointsToRectangularFitArray(lines), 2);
        }

        public static ConstrainedLeastSquareFit FitLine(IEnumerable<PlanePoint> points)
        {
            return new ConstrainedLeastSquareFit(
                PointsToArray(points.ToList()),
                2
                );
        }

        public ConstrainedLeastSquareFit(double[,] A, int dim)
        {
            //% solves the constrained least squares Problem
            //% A(c n)' ~ 0 subject to norm(n,2)=1
            //% length(n) = dim
            //% [c, n] = clsq(A, dim)
            int m = A.GetLength(0); // row count
            int p = A.GetLength(1); // col count
            if (p < dim + 1)
                throw new Exception("not enough unknowns");

            if (m < dim)
                throw new Exception("not enough equations");

            m = Math.Min(m, p);

            //var lotr = new Accord.Math.Decompositions.QrDecomposition(A);

            //MatrixExpert.MatrixDecompose()

            var R = new Accord.Math.Decompositions.QrDecomposition(A).UpperTriangularFactor;
            // do triu?
            //R = triu(qr (A));

            var subR = Matrix.Submatrix(R,
                p - dim, m - 1, //p - dim + 1, m
                p - dim, p - 1 //p - dim + 1,p
                );

            var svd = new Accord.Math.Decompositions.SingularValueDecomposition(subR);
            //[U, S, V] = svd(R(, ));
            var V = svd.RightSingularVectors;

            double[,] N = Matrix.GetColumns(V, new int[] { dim - 1 });
            n = N.GetColumn(0);

            //var skod000 = Accord.Math.Matrix.Submatrix(R,
            //    );

            double[,] a = Matrix.Submatrix(R, 0, p - dim - 1, 0, p - dim - 1);
            //double[,] aNeg = Matrix.Multiply(-1, a);
            double[,] b = Matrix.Submatrix(R, 0, p - dim - 1, p - dim, p - 1);
            //double[,] bTimesN = Matrix.Multiply(b, N);

            double[,] xForABSolved = Matrix.Solve(a, b, leastSquares: false);

            double[,] xForABSolvedNegated = MatrixExpert.Negate(xForABSolved);
            //int rowCount = xForABSolved.GetLength(0); // should be a column vector
            //var negateMatrix = new double[rowCount,1];
            //for (int i = 0; i < rowCount; i++)
            //    negateMatrix[i,0] = -1;

            //double[,] xForABSolvedNegated = Matrix.Multiply(
            //        negateMatrix,  //-1, 
            //        xForABSolved
            //        );

            c = Matrix.Multiply(xForABSolvedNegated, n);
            //c = Matrix.Multiply(xForABSolvedNegated, n);


            //c = Matrix.Divide(bTimesN, aNeg).GetColumn(0);

            //= Accord.Math.Matrix.Multiply(
            //-1,
            //    Accord.Math.Matrix.Divide(
            //        Accord.Math.Matrix.Submatrix(R, 0, p - dim - 1, 0, p - dim - 1),
            //                        Accord.Math.Matrix.Multiply(
            //        Accord.Math.Matrix.Submatrix(R, 0, p - dim - 1, p - dim, p - 1),
            //        n
            //        )

            //    )
            //);

            //c

            //c = -R(1:p - dim, 1:p - dim)\R(1:p - dim, p - dim + 1:p) * n;
            //Let us test the function clsq with the following main
        }

        #endregion
        #region Methods

        private static double[,] PointsToOrthogonalLinesFitArray(List<PlanePoint> l1, List<PlanePoint> l2)
        {
            return PointsToArray(
                l1,
                GetOrthogonalPoints(l2)
                );
        }

        private static double[,] PointsToRectangularFitArray(RectLinePoints<PlanePoint> lines)//List<PlanePoint> top, List<PlanePoint> right, List<PlanePoint> bottom, List<PlanePoint> left)
        {
            return PointsToArray(
                lines.Top.ToList(),
                GetOrthogonalPoints(lines.Right),
                lines.Bottom.ToList(),
                GetOrthogonalPoints(lines.Left)
                );
        }

        private static List<PlanePoint> GetOrthogonalPoints(IEnumerable<PlanePoint> l)
        {
            return l.Select(p => new XYPoint(p.Y, -p.X)).Cast<PlanePoint>().ToList(); // standard geometry, we rotate the point by 90 degrees;
        }

        private static double[,] PointsToArray(params IList<PlanePoint>[] sets)
        {
            int pointCount = sets.Select(s => s.Count).Sum();
            var A = new double[pointCount, sets.Length + 2];

            int rowIndex = 0;
            for (int setIndex = 0; setIndex < sets.Count(); setIndex++)
            {
                var set = sets[setIndex];
                foreach (var point in set)
                {
                    A.SetValue(1, rowIndex, setIndex);
                    A.SetValue(point.X, rowIndex, sets.Length);
                    A.SetValue(point.Y, rowIndex, sets.Length + 1);

                    rowIndex++;
                }
            }

            return A;
        }


        //private static double[,] PointsToLineFitArray(IList<PlanePoint> points)
        //{
        //    int pointCount = points.Count();

        //    var A = new double[pointCount, 3];
        //    for (int rowIndex = 0; rowIndex < pointCount; rowIndex++)
        //    {
        //        A.SetValue(1, rowIndex, 0);
        //        A.SetValue(points[rowIndex].X, rowIndex, 1);
        //        A.SetValue(points[rowIndex].Y, rowIndex, 2);
        //    }

        //    return A;
        //}

        #endregion


    }
}
