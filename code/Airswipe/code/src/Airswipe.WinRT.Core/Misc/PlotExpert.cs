using Airswipe.WinRT.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Airswipe.WinRT.Core.Misc
{
    public static class PlotExpert
    {
        public static string WAPlotPointSetConnected<T>(IEnumerable<T> points)
        {
            return
                "{" +
                points.Select((d, i) => (i % 2 == 0) ? "{" + d : d + "}").Aggregate((s1, s2) => s1 + "," + s2) +
                "}";
        }

        public static string TexPointData(string label = "A", params PointComponents[] points)
        {
            return PointsToTexScatterPointCsvData(points, label);
        }

        public static string PointsToTexPlaneProjectedPoints(IEnumerable<SpatialPoint> points, string planeName = "myplane", string pointOptions = "color = orange", string  projectedPointOptions = "", string projectionLineOptions = "->, shorten <= 1pt, shorten >= 1pt, color = gray")
        {
            return points.Select(
                (p, i) => string.Format(
                    "\t\\definePointByXYZ{{{0}}}{{{1}}}{{{2}}}{{{3}}};\r\n\t\\draw[{4}]({0}) circle[radius = 1pt];\r\n",
                    "p" + i,
                    p.X, p.Y, p.Z,
                    pointOptions
                    ) 
                ).Select(
                    (s, i) => s + string.Format(
                        "\t\\projectPointToPlaneAlongZ{{proj{1}}}{{{1}}}{{{0}}}\r\n" +
                        "\t\\fill[{2}](proj{1}) circle [radius=1pt];\r\n" +
	                    "\t\\draw[{3}]({1}) -- (proj{1});\r\n",
                        planeName, 
                        "p" + i,
                        projectedPointOptions,
                        projectionLineOptions
                        )
                    ).Aggregate(
                        (s1, s2) => s1 + "\r\n" + s2
                        );
        }

        public static string PointsToTexScatterPointCsvData(IEnumerable<PointComponents> points, string label = "A")
        {
            if (points.Count() == 0)
                throw new Exception("no points to process");

            var componentNames = new string[] { "x", "y", "z" }.Take(
                points.First().Components.Count()
                ).Concat(new string[] { "label" });


            return new List<IEnumerable<string>> { componentNames }.Concat(
                points.Select(
                    p => p.Components.Select(c => c.ToString()).Concat(new string[] { label })
                    )
                ).Select(
                s => s.Aggregate(
                    (s1,s2) => s1 + "\t" + s2
                    )
                ).Aggregate((l1,s2) => l1 + "\r\n" + s2);

            //string result = "x\ty\tlabel\r\n";

            //foreach (var point in points)
            //    result += point.Components.Select(c => c.ToString()).Concat(new string[] { label }).Aggregate((s1, s2) => s1 + ",\t" + s2);

            //return result;
        }

    }
}
