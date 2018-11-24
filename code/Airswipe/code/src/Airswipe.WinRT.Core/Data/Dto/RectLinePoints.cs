using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Airswipe.WinRT.Core.Data.Dto
{
    public class RectLinePoints<T> : Dictionary<RectLineBoundary, IEnumerable<T>> where T : PointComponents
    {
        public static RectLinePoints<T> Init<N>(double[,] top = null, double[,] right = null, double[,] bottom = null, double[,] left = null) where N : PointComponents, new() 
        {
            return new RectLinePoints<T>
            {
                Top = ToComponents<N>(top).Cast<T>(),
                Right = ToComponents<N>(right).Cast<T>(),
                Bottom = ToComponents<N>(bottom).Cast<T>(),
                Left = ToComponents<N>(left).Cast<T>()
            };
        }

        private static IEnumerable<N> ToComponents<N>(double[,] componentSets) where N : PointComponents, new()
        {
            if (componentSets != null)
            {
                int rows = componentSets.GetLength(0);
                int cols = componentSets.GetLength(1);

                var l = new List<IEnumerable<double>>();
                for (int row = 0; row < rows; row++)
                {
                    var components = new List<double>();
                    for (int col = 0; col < cols; col++)
                        components.Add(componentSets[row, col]);

                    var t = new N();
                    t.Components = components;
                    yield return t;
                }
            }
        }


        public RectLinePoints<N> Apply<N>(Func<T, N> f) where N : PointComponents
        {
            return new RectLinePoints<N>
            {
                Top = Top.Select(p => f(p)),
                Right = Right.Select(p => f(p)),
                Bottom = Bottom.Select(p => f(p)),
                Left = Left.Select(p => f(p))
            };
        }

        //public RectLinePoints<N> CastPoints<N, T>(this RectLinePoints<T> source) where T : N where N : PointComponents
        //{
        //    return source.Apply(p => (N)p);
        //}

        public RectLinePoints<N> CastPoints<N>() where N : PointComponents
        {
            return new RectLinePoints<N>
            {
                Top = Top.Cast<N>(),
                Right = Right.Cast<N>(),
                Bottom = Bottom.Cast<N>(),
                Left = Left.Cast<N>()
            };
        }

        public IEnumerable<T> Top
        {
            get { return this[RectLineBoundary.Top]; }
            set { this[RectLineBoundary.Top] = value; }
        }

        public IEnumerable<T> Right
        {
            get { return this[RectLineBoundary.Right]; }
            set { this[RectLineBoundary.Right] = value; }
        }

        public IEnumerable<T> Bottom
        {
            get { return this[RectLineBoundary.Bottom]; }
            set { this[RectLineBoundary.Bottom] = value; }
        }

        public IEnumerable<T> Left
        {
            get { return this[RectLineBoundary.Left]; }
            set { this[RectLineBoundary.Left] = value; }
        }


        public IEnumerable<T> Concatenated
        {
            get { return Values.Aggregate((l1, l2) => l1.Concat(l2)); }
        }

        //public int PointCount
        //{
        //    get { return Values.Count(); }
        //}
    }
}
