namespace Airswipe.WinRT.Core.Data
{
    public interface Rectangle<TPoint>//, Plane<TPoint>
    {
        TPoint Origo { get; set; }

        //TPoint ToClosestEdge { get; set; } // which is implicitly the longest of the two orthogonal sets of edges

        //double HorisontalOverVerticalRatio { get; set; }

        double SideRatio { get; set; }

        TPoint OrigoToRight { get; set; } // which is implicitly the longest of the two orthogonal sets of edges

        TPoint OrigoToBottom { get; set; } // which is implicitly the longest of the two orthogonal sets of edges

        TPoint GetJoint(RectJointBoundary b);

        double Width { get; }
    }
}
