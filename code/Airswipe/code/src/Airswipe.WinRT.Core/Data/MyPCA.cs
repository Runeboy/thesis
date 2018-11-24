using Accord.Statistics.Analysis;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Airswipe.WinRT.Core.Data
{
    public class Component : XYPoint
    {
        public double Eigenvalue { get; set; }
    }


    public class MyPCA
    {

        public const double Confidence95 = 5.991; //2.4476519360399264088826733168882; // ie. sqrt of 5.991
        public const double Confidence90 = 4.605;

        public MyPCA() { }

        //public MyPCA() { }


        public MyPCA(double[][] data)
        {
            InitializeFromData(data);
        }

        public MyPCA(IEnumerable<PlanePoint> data) : this(PointsToDouble(data)) { }


        public static double[][] PointsToDouble(IEnumerable<PlanePoint> data)
        {
            int rowCount = data.Count();
            double[][] matrix = new double[rowCount][];
            for (int i = 0; i < rowCount; i++)
            {
                matrix[i] = new double[2];
                matrix[i][0] = data.ElementAt(i).X;
                matrix[i][1] = data.ElementAt(i).Y;
            }

            return matrix;
        }

        public static double[][] PointsToDouble(double[,] data)
        {
            int rowCount = data.GetLength(0);
            double[][] matrix = new double[rowCount][];
            for (int i = 0; i < rowCount; i++)
            {
                matrix[i] = new double[2];
                matrix[i][0] = data[i, 0];
                matrix[i][1] = data[i, 1];
            }

            return matrix;
        }

        public double[][] Data
        {
            set
            {
                InitializeFromData(value);
            }
        }


        private void InitializeFromData(double[][] data)
        {
            PrincipalComponentAnalysis PCA = new PrincipalComponentAnalysis(data,
                   //AnalysisMethod.Standardize
                   AnalysisMethod.Center
                   );
            PCA.Compute();

            IsXInverted = AreSignsOpposite(PCA.Components[1].Eigenvector[0], -PCA.Components[0].Eigenvector[1]);


            Mean = new XYPoint(
                      PCA.Means[0],
                      PCA.Means[1]);

            Eig1VectorUnit = new Component
            {
                X = PCA.Components[0].Eigenvector[0] * (IsXInverted ? -1 : 1),
                Y = PCA.Components[0].Eigenvector[1] * (IsXInverted ? -1 : 1),
                Eigenvalue = PCA.Eigenvalues[0]
            };

            Eig2VectorUnit = new Component
            {
                X = PCA.Components[1].Eigenvector[0],
                Y = PCA.Components[1].Eigenvector[1],
                Eigenvalue = PCA.Eigenvalues[1]
            };
        }

        public bool IsXInverted { get; set; }

        private static bool AreSignsOpposite(double a, double b)
        {
            return a >= 0 && b < 0 || b >= 0 && a < 0;
        }

        //public double[,] Transform(double[,] data)
        //{
        //    return PCA.Transform(data);
        //}

        public PlanePoint ScalePointByAngledEllipse(PlanePoint p, double scale, bool invert = false)
        {
            //var meanAdjusted = p;
            var meanAdjusted = p.Subtract(Mean);
            //var meanAdjusted = p.Subtract(Mean);///p.Add(Mean);// (subtractMean? p.Subtract(Mean) : p);
            //var meanAdjusted = p;//.Add(Mean);

            var transformed = new XYPoint
            {
                X = meanAdjusted.Dot(Eig1VectorUnit),
                Y = meanAdjusted.Dot(Eig2VectorUnit),
            };

            //var directionOfPoint = p.Normalize();
            //var angleWithFirstComp = Math.Acos(directionOfPoint.Dot(Eig1VectorUnit));

            //p.X = Math.Cos(angleWithFirstComp) * Math.Sqrt(Eig1VectorUnit.Eigenvalue);
            //p.Y = Math.Sin(angleWithFirstComp) * Math.Sqrt(Eig2VectorUnit.Eigenvalue);


            //p.X = Math.Cos(angleWithFirstComp) * Math.Sqrt(Eig1VectorUnit.Eigenvalue);
            //p.Y = Math.Sin(angleWithFirstComp) * Math.Sqrt(Eig2VectorUnit.Eigenvalue);

            PlanePoint transformedScaled = new XYPoint
            {
                X = transformed.X * (invert? scale  / Math.Sqrt(Eig1VectorUnit.Eigenvalue) :  Math.Sqrt(Eig1VectorUnit.Eigenvalue) / scale),
                Y = transformed.Y * (invert ? scale / Math.Sqrt(Eig2VectorUnit.Eigenvalue) : Math.Sqrt(Eig2VectorUnit.Eigenvalue) / scale)
            };

            

            //p.X = Math.Sqrt(Eig1VectorUnit.Eigenvalue) ;
            //p.Y = Math.Sqrt(Eig2VectorUnit.Eigenvalue) ;


            //if (subtractMean)
            //    transformedScaled = transformedScaled.Add(Mean);
            //transformedScaled = transformedScaled.Subtract(Mean);


            var transformedBack = new XYPoint
            {
                X = transformedScaled.Dot(new XYPoint(Eig1VectorUnit.X, -Eig1VectorUnit.Y)),
                Y = transformedScaled.Dot(new XYPoint(-Eig2VectorUnit.X, Eig2VectorUnit.Y))
            };


            var angle = Math.Acos(transformedBack.Normalize().Dot(Mean.Normalize()));

            var result = transformedBack
                .Add(Mean)
                .Add(Mean.Multiply((p.Length) / scale));

            return result;
            //.Subtract(Mean);
            //return (subtractMean? transformedBack.Subtract(Mean) : transformedBack);//.Subtract(Mean);
            //p.X -= Mean.X;
            //p.Y -= Mean.Y;
        }

        //public double[][] Transform(double[][] data)
        //{
        //    var transformed = PCA.Transform(data);

        //    if (IsXInverted)
        //        foreach (double[] row in transformed)
        //            row[0] = -row[0];

        //    return transformed;
        //}

        public double SkewRatio
        {
            get { return Eig2VectorUnit.Eigenvalue / Eig1VectorUnit.Eigenvalue; }
        }

        //public double[][] Data
        //{
        //    //get;
        //    set
        //    {

        //        //var abe = Eig1Vector.Multiply(1 / pca.Eigenvalues.Sum());
        //        //var abe2 = Eig2Vector.Multiply(1 / pca.Eigenvalues.Sum());
        //    }
        //}

        public PlanePoint Mean { get; set; }


        public Component Eig1VectorUnit { get; set; }
        //{
        //    get
        //    {
        //        return new Component
        //        {
        //            X = PCA.Components[0].Eigenvector[0] * (IsXInverted ? -1 : 1),
        //            Y = PCA.Components[0].Eigenvector[1] * (IsXInverted ? -1 : 1),
        //            Eigenvalue = PCA.Eigenvalues[0]
        //        };
        //    }
        //}

        public Component Eig2VectorUnit { get; set; }
        //{
        //    get
        //    {
        //        return new Component
        //        {
        //            X = PCA.Components[1].Eigenvector[0],
        //            Y = PCA.Components[1].Eigenvector[1],
        //            Eigenvalue = PCA.Eigenvalues[1]
        //        };
        //    }
        //}

        //public PlanePoint Eig1Vector { get { return Eig1VectorUnit.Multiply(PCA.Components[0].Eigenvalue); } }
        //public PlanePoint Eig2Vector { get { return Eig2VectorUnit.Multiply(PCA.Components[1].Eigenvalue); } }

        public double PrincipalAngle
        {
            get
            { return Math.Acos(Eig1VectorUnit.X); }
        }

        public double PrincipalAngleDegree
        {
            get { return GeometryExpert.RadianToDegree(PrincipalAngle); }
        }

        //[JsonIgnore]
        //private PrincipalComponentAnalysis PCA
        //{
        //    get
        //    {
        //        if (pca == null)
        //            throw new Exception("Data not set");

        //        return pca;
        //    }
        //}

        //public PrincipalComponent FirstComponent { get { return PCA.Components[0]; } }

        //public PrincipalComponent SecondComponent { get { return PCA.Components[1]; } }
    }
}
