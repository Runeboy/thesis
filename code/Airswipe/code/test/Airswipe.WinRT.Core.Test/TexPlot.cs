using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.Data.Dto;
using Airswipe.WinRT.Core.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airswipe.WinRT.Core.Test
{
    [TestClass]
    public class TexPlot
    {
        [TestMethod]
        public void TestFitLine()
        {
            string csv = PlotExpert.PointsToTexScatterPointCsvData(
                CreateSinePlaneFitPoints()
                );

            var str = PlotExpert.PointsToTexPlaneProjectedPoints(
                CreateSinePlaneFitPoints(),
                //pointOptions: @"\projectionPointOptions",
                //projectedPointOptions: @"\projectedPointOptions",
                //projectionLineOptions: @"\projectionLineOptions"
                pointOptions: "color=red!50",
                projectedPointOptions: "color=black!80",
                projectionLineOptions: "->, shorten <= 1pt, shorten >= 1pt, color = gray!60"
                );
        }

        public static IEnumerable<SpatialPoint> CreateSinePlaneFitPoints()
        {
            var r = new Random(123);
            double start = 0.3;
            double end = 2;
            double span = end - start;
            double revolutions = 2;
//            double zFluctuation = 0.8;
            double zFluctuation = 0.3;

            var normal = new XYZPoint(1, 1, 1);
            var fluct = new XYZPoint(-1, 1, 0); //1, 1, 1).Cross(new XYZPoint( 1.5, 1.5, -3)).Normalize();

            double maxZ = 3;

            //for (double p = 0; p < span; p += 0.05)
            for (double z = maxZ; z > 0; z -= 0.05)
                {
                    yield return new XYZPoint
                {
                    X = (3 - z)/2,
                    Y = (3 - z)/2,
                    Z = z
                    //X = start + p,
                    //Y = start + p,
                    //Z = (3 - 2 * (start + p))
                }
                    .Add(
                    fluct.Multiply(Math.Sin(z * (revolutions * Math.PI * 2 / span)) * ((maxZ - z) / maxZ))
                    )
                    .Add(
                        //normal.Multiply(zFluctuation * (-.5 + r.NextDouble()))
                        normal.Multiply(zFluctuation * (r.NextGaussian(mu:0, sigma:1))) 
                        //new XYZPoint(0,0, fluctuation * (-.5 + r.NextDouble()))
                        );
            }
        }

    }
}
