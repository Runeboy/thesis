using Microsoft.VisualStudio.TestTools.UnitTesting;
using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.Data.Dto;
using System;
using System.Linq;
using Airswipe.WinRT.Core.Fitting;
using System.Collections.Generic;

namespace Airswipe.WinRT.Core.Test
{
    [TestClass]
    public class ConstrainedLinearSquareFitTest
    {
        [TestMethod]
        public void TestFitLine()
        {
            var Py = new double[] { 0.2, 1.0, 2.6, 3.6, 4.9, 5.3, 6.5, 7.8, 8.0, 9.0 };
            var Px = Enumerable.Range(1, Py.Length).Select(i => (double)i).ToArray();

            var clsq = ConstrainedLeastSquareFit.FitLine(
                Px.Select((x, i) => new XYPoint(x, Py[i])).Cast<PlanePoint>().ToArray()
                );

            Assert.AreEqual(1, clsq.c.Length);
            Assert.AreEqual(0.4162, Math.Round(clsq.c[0], 4));

            Assert.AreEqual(2, clsq.n.Length);
            Assert.AreEqual(-0.7057, Math.Round(clsq.n[0], 4));
            Assert.AreEqual(0.7086, Math.Round(clsq.n[1], 4));

            //      % mainline.m
            //Px = [1:10]'
            //Py = [0.2 1.0 2.6 3.6 4.9 5.3 6.5 7.8 8.0 9.0]'
            //A = [ones(size(Px)) Px Py]
            //     [c, n] = clsq(A, 2)
        }

        [TestMethod]
        public void TestFitParallelLines()
        {
            var Py = new double[] { 0.2, 1.0, 2.6, 3.6, 4.9, 5.3, 6.5, 7.8, 8.0, 9.0 };
            var Px = Enumerable.Range(1, Py.Length).Select(i => (double)i).ToArray();

            var Qy = new double[] { 5.8, 7.2, 9.1, 10.5, 10.6, 10.7, 13.4, 14.2, 14.5 };
            var Qx = new double[] { 1.5, 2.6, 3.0, 4.3, 5.0, 6.4, 7.6, 8.5, 9.9 };
            Assert.AreEqual(Qx.Length, Qy.Length);


            //A = [ones(size(Px))  zeros(size(Px)) Px Py
            //    zeros(size(Qx))  ones(size(Qx)) Qx Qy]
            //[c, n] = clsq(A, 2)
            //clf; hold on;
            //         axis([-1 11 - 1 17])
            //plotline(Px, Py, 'o', c(1), n, '-')
            //plotline(Qx, Qy, '+', c(2), n, '-')
            //hold off;

            var clsq = ConstrainedLeastSquareFit.FitParallelLines(
                Px.Select((x, i) => new XYPoint(x, Py[i])).Cast<PlanePoint>().ToArray(),
                Qx.Select((x, i) => new XYPoint(x, Qy[i])).Cast<PlanePoint>().ToArray()
                );

            Assert.AreEqual(2, clsq.c.Length);
            Assert.AreEqual(0.5091, Math.Round(clsq.c[0], 4));
            Assert.AreEqual(-3.5877, Math.Round(clsq.c[1], 4));

            Assert.AreEqual(2, clsq.n.Length);
            Assert.AreEqual(-0.7146, Math.Round(clsq.n[0], 4));
            Assert.AreEqual(0.6996, Math.Round(clsq.n[1], 4));

        }

        [TestMethod]
        public void TestFitOrthogonalLines()
        {
            var Py = new double[] { 0.2, 1.0, 2.6, 3.6, 4.9, 5.3, 6.5, 7.8, 8.0, 9.0 };
            var Px = Enumerable.Range(1, Py.Length).Select(i => (double)i).ToArray();

            var Qy = new double[] { 12, 8, 6, 3, 3, 0 };
            var Qx = new double[] { 0, 1, 3, 5, 6, 7 };
            Assert.AreEqual(Qx.Length, Qy.Length);

            //A = [ones(size(Px))  zeros(size(Px)) Px Py
            //    zeros(size(Qx))  ones(size(Qx)) Qx Qy]
            //[c, n] = clsq(A, 2)
            //clf; hold on;
            //         axis([-1 11 - 1 17])
            //plotline(Px, Py, 'o', c(1), n, '-')
            //plotline(Qx, Qy, '+', c(2), n, '-')
            //hold off;

            var clsq = ConstrainedLeastSquareFit.FitOrthogonalLines(
                Px.Select((x, i) => new XYPoint(x, Py[i])).Cast<PlanePoint>().ToArray(),
                Qx.Select((x, i) => new XYPoint(x, Qy[i])).Cast<PlanePoint>().ToArray()
                );

            Assert.AreEqual(2, clsq.c.Length);
            Assert.AreEqual(-0.2527, Math.Round(clsq.c[0], 4));
            Assert.AreEqual(6.2271, Math.Round(clsq.c[1], 4));

            Assert.AreEqual(2, clsq.n.Length);
            Assert.AreEqual(-0.6384, Math.Round(clsq.n[0], 4));
            Assert.AreEqual(0.7697, Math.Round(clsq.n[1], 4));
        }


        [TestMethod]
        public void TestFitRectangularLines()
        {
            var lines = RectLinePoints<PlanePoint>.Init<XYPoint>(
                top: new double[,] { { 1, 6 }, { 1.2, 6.1 }, { 0.5, 7.1 }, { 1, 8 }, { 2, 9 }, { 3, 9.3 } },
                right: new double[,] { { 3, 9.1 }, { 4, 9 }, { 5, 8 }, { 5.5, 7.5 }, { 6, 7 }, { 7, 6 } },
                bottom: new double[,] { { 6.3, 5.2 }, { 6, 5 }, { 4, 4.6 }, { 3.5, 2.7 }, { 2.4, 2.6 }, { 2, 1 } },
                left: new double[,] { { 1.1, 1 }, { 0, 2.1 }, { -1.8, 3 }, { -3, 5 } }
                );

            //double[,] top =  {
            //    { 1,6 }, { 1.2, 6.1 }, {0.5, 7.1}, { 1, 8 }, { 2,9 }, {3,9.3}
            //};
            //double[,] right =  {
            //    { 3, 9.1 }, { 4,9 }, {  5,8 }, { 5.5, 7.5  }, { 6,7 }, {  7,6}
            //};
            //double[,] bottom =  {
            //    { 6.3, 5.2 }, { 6,5 }, { 4,4.6 }, { 3.5, 2.7  }, { 2.4, 2.6 } , { 2, 1}
            //};
            //double[,] left =  {
            //    { 1.1,1 }, { 0,2.1 }, { -1.8, 3 }, { -3, 5  }
            //};
            Func<double[,], IEnumerable<PlanePoint>> toPlanePoints = (l) => Enumerable.Range(0, l.GetLength(0)).Select(rowIndex => new XYPoint(l[rowIndex, 0], l[rowIndex, 1])).Cast<PlanePoint>();

            //var clsq = ClSq.FitRectangleLines(toPlanePoints(top), toPlanePoints(right), toPlanePoints(bottom), toPlanePoints(left));
            ConstrainedLeastSquareFit clsq = ConstrainedLeastSquareFit.FitRectangleLines(lines);


            //Assert.AreEqual(2, clsq.c.Length);
            //Assert.AreEqual(-0.2527, Math.Round(clsq.c[0], 4));
            //Assert.AreEqual(6.2271, Math.Round(clsq.c[1], 4));

            //Assert.AreEqual(2, clsq.n.Length);
            //Assert.AreEqual(-0.6384, Math.Round(clsq.n[0], 4));
            //Assert.AreEqual(0.7697, Math.Round(clsq.n[1], 4));

            // verify visually with rounded numbers

            //Round(clsq);

            string p = "plot {0} + ({1}x) + ({2}y) = 0,";
            string s = "";
            s += string.Format(p, clsq.c[0], clsq.n[0], clsq.n[1]);
            s += string.Format(p, clsq.c[1], -clsq.n[1], clsq.n[0]);
            s += string.Format(p, clsq.c[2], clsq.n[0], clsq.n[1]);
            s += string.Format(p, clsq.c[3], -clsq.n[1], clsq.n[0]);

            string url = "https://www.wolframalpha.com/input/?i=" + Uri.EscapeDataString(s);
        }

        private static void Round(ConstrainedLeastSquareFit clsq)
        {
            clsq.n[0] = Math.Round(clsq.n[0], 5);
            clsq.n[1] = Math.Round(clsq.n[1], 5);

            clsq.c[0] = Math.Round(clsq.c[0], 5);
            clsq.c[1] = Math.Round(clsq.c[1], 5);
            clsq.c[2] = Math.Round(clsq.c[2], 5);
            clsq.c[3] = Math.Round(clsq.c[3], 5);
        }
    }
}
