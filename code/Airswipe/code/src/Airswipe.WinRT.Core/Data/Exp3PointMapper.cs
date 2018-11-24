using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.Misc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Airswipe.WinRT.Core.Data
{
    public class Exp3PointMapper
    {
        private MyPCA pca;

        public Exp3PointMapper()
        {
        }

        //public Exp3PointMapper(MyPCA pca)
        //{
        //    PCA = pca;
        //}

        public Exp3PointMapper(IEnumerable<PlanePoint> data)
        {
            PCA = new MyPCA(data);
        }

        public PlanePoint FromModeledToNormalSpace(PlanePoint from)
        {
            //var mapper = StringExpert.FromJson<Exp3PointMapper>("{  \"PCA\": {    \"IsXInverted\": true,    \"SkewRatio\": 0.73233363551820507,    \"Mean\": {      \"X\": 108.68019809722901,      \"Y\": 322.33007300694783    },    \"Eig1VectorUnit\": {      \"Eigenvalue\": 4642996.7584800767,      \"X\": -0.90349918621313086,      \"Y\": 0.42858980448933959    },    \"Eig2VectorUnit\": {      \"Eigenvalue\": 3400222.6958369561,      \"X\": -0.42858980448933959,      \"Y\": -0.90349918621313086    },    \"PrincipalAngle\": 2.6986612710039029,    \"PrincipalAngleDegree\": 154.62190116393413  }}");

            var oneStep = FromNormalToModeledSpace(from);
            var twoStep = FromNormalToModeledSpace(oneStep);

            var firstStepOut = from.Subtract(oneStep);
            var secondStepOut = oneStep.Subtract(twoStep);

            var angle = Math.Acos(
                firstStepOut.Normalize().Dot(secondStepOut.Normalize())
                );

            var scale = firstStepOut.Length / secondStepOut.Length;

            ////var dirChange =
            //var toAdd = new XYPoint(
            //    Math.Cos(angle) * firstStepOut.X,
            //    Math.Sin(angle) * firstStepOut.Y
            //    ); 

            var dirChange = firstStepOut
                //.Multiply(1 / scale)
                .Subtract(secondStepOut.Multiply(scale));
            //.Subtract(secondStepOut);

            var approx = from.Add(
                firstStepOut.Multiply(scale)
                );

            return approx.Add(dirChange);
        }

        public PlanePoint FromNormalToModeledSpace(PlanePoint p, bool isToMapWithSkew = true, bool invert = false)
        {
            //var radiusPixels = TestPage.TICK_DISTANCE_PIXELS * 50;

            var transformed = PCA.ScalePointByAngledEllipse(p, CircleRadius, invert);

            if (isToMapWithSkew)
                transformed = Skew(p, transformed);

            return transformed;
        }


        //public PlanePoint DetransformPoint(PlanePoint p, bool invert = false)
        //{
        //    if (TransformMap == null)
        //        throw new Exception("Transform map not dervied.");


        //}

        private PlanePoint Skew(PlanePoint source, PlanePoint transformed)
        {
            // ******************** BEGIN APPLY ARC SKEW
            var opp = PCA.ScalePointByAngledEllipse(source.Multiply(-1), CircleRadius);

            var toOpp = opp.Subtract(transformed);

            var aLength = transformed.Multiply(-1).Dot(toOpp.Normalize());
            var a = toOpp.Normalize().Multiply(aLength).Multiply(-1);
            var b = transformed.Add(a.Multiply(-1)).Multiply(-1);

            var angle = Math.Acos(
                toOpp.Normalize().Dot(transformed.Multiply(-1).Normalize())
                );

            ////var radius = ((Math.PI / 4) / angle) * b.Length;
            //var radius = ((Math.PI / 4) / angle) * b.Length;
            ////var circum = 2 * Math.PI * radius;

            var r50 = 8.5169291338582677165354330708661 * 10 * 50;
            //var rOMF = 8.5169291338582677165354330708661 * 10 * 50;

            //var prop = transformed.Length / (CircleRadius * 2 * Math.PI); //(5000 * 2 ); //
            //var prop = (transformed.Length / Math.Sign(angle)) / (CircleRadius * 2 * Math.PI); //(5000 * 2 ); //
            //var prop = (transformed.Length / Math.Sign(angle)) / (CircleRadius * 2 * Math.PI); //(5000 * 2 ); //

            //var prop = (transformed.Length) / (PCA.ScalePointByAngledEllipse(
            //    source.Normalize().Multiply(CircleRadius),
            //    //source.Normalize().Multiply(r50),
            //    r50
            //    ).Length);

            //var prop = source.Length / CircleRadius; //r50;
            var prop = source.Length / r50;

            //(toOpp.Length * Math.PI);

            //return transformed;

            //return b.Multiply(-1).Add(
            //            a.Multiply(1)
            //            );


            return transformed.Add(b.Multiply(1 - prop));

            transformed =
                    //b.Multiply(-1).Add( // this line kills the skrew in top-left!
                    b.Multiply(-Math.Sin(prop)).Add(
                        a.Multiply(Math.Cos(prop))
                        );
            //);

            //radiusPixels, // make 
            //);
            // ******************** END APPLY SKEW
            return transformed;
        }

        //private PlanePoint Deskew(PlanePoint p, PlanePoint transformed)
        //{
        //    // ******************** BEGIN APPLY ARC SKEW
        //    var opp = PCA.ScalePointByAngledEllipse(p.Multiply(-1), CircleRadius);

        //    var toOpp = opp.Subtract(transformed);

        //    var aLength = transformed.Multiply(-1).Dot(toOpp.Normalize());
        //    var a = toOpp.Normalize().Multiply(aLength).Multiply(-1);
        //    var b = transformed.Add(a.Multiply(-1)).Multiply(-1);

        //    var angle = Math.Acos(
        //        toOpp.Normalize().Dot(transformed.Multiply(-1).Normalize())
        //        );

        //    var radius = ((Math.PI / 4) / angle) * b.Length;
        //    var circum = 2 * Math.PI * radius;

        //    var prop = (transformed.Length) / (CircleRadius * 2 * Math.PI); //(5000 * 2 ); //

        //    //(toOpp.Length * Math.PI);

        //    transformed =
        // //b.Multiply(-1).Add( // this line kills the skrew in top-left!
        // b.Multiply(Math.Sin(prop)).Add(a.Multiply(Math.Cos(prop)));

        //    ///////////////////////
        //    //);

        //    //radiusPixels, // make 
        //    //);
        //    // ******************** END APPLY SKEW
        //    return transformed;
        //}

        [JsonIgnore]
        public double RadiusX { get { return Math.Sqrt(PCA.Eig1VectorUnit.Eigenvalue); } }

        [JsonIgnore]
        public double RadiusY { get { return Math.Sqrt(PCA.Eig2VectorUnit.Eigenvalue); } }

        [JsonIgnore]
        public double CircleRadius { get { return MathExpert.GetCircleRadius(MathExpert.EllipsePerimeteRamanujanrApprox(RadiusX, RadiusY)); } }

        [JsonIgnore]
        private static readonly ILogger log = new TypeLogger<Exp3PointMapper>();

        [JsonIgnore]
        public double EllipseAngleDegrees
        {
            get { return GeometryExpert.RadianToDegree( 
                Math.Asin( PCA.Eig1VectorUnit.Y / PCA.Eig1VectorUnit.X)
                ); }
        }

        //public short[][] TransformMapX { get; set; }
        //public short[][] TransformMapY { get; set; }
        ////public double[,] TransformMapY { get; set; }

        //public PlanePoint DetransformPoint(PlanePoint p)
        //{
        //    if (TransformMapX == null || TransformMapY == null)
        //        throw new Exception("Transform map not set (is PCA set?).");

        //    var x = (int)Math.Round(p.X);
        //    var y = (int)Math.Round(p.Y);


        //    var iX = (x + RADIUS) / mesh;
        //    var iY = (y + RADIUS) / mesh;

        //    return new XYPoint(
        //        TransformMapX[iX][iY],
        //        TransformMapX[iX][iY]
        //        );
        //}

        //int RADIUS = 10000;
        //int mesh = 10;

        //public void DeriveTransformMap()
        //{
        //    log.Info("Deriving vector map..");
        //    int rowSize = (2 * RADIUS / mesh + 1);
        //    //int size = rowSize * rowSize;

        //    TransformMapX = new short[rowSize][];
        //    TransformMapY = new short[rowSize][];

        //    for (int x = -RADIUS; x <= RADIUS; x++)
        //    {
        //        if (x % 1000 == 0)
        //            log.Verbose("\t x = " + x);

        //        for (int y = -RADIUS; y <= RADIUS; y += 10)
        //        {
        //            //if (y % 1000 == 0)
        //            //    log.Verbose("\t\t y = " + y);

        //            var p = GetTransformedPoint(new XYPoint(x, y));

        //            var iX = (x + RADIUS) / mesh;
        //            var iY = (y + RADIUS) / mesh;

        //            if (TransformMapX[iX] == null)
        //                TransformMapX[iX] = new short[rowSize];
        //            if (TransformMapY[iY] == null)
        //                TransformMapY[iY] = new short[rowSize];

        //            TransformMapX[iX][iY] = (short)Math.Round(p.X);
        //            TransformMapY[iX][iY] = (short)Math.Round(p.Y);
        //        }
        //    }
        //}

        public MyPCA PCA
        {
            get
            {
                //if (pca == null)
                //    throw new Exception("PCA data has not been set.");
                return pca;
            }
            set
            {
                pca = value;

            }
        }

    }
}
