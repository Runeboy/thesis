using Airswipe.WinRT.Core.Data;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Airswipe.WinRT.UI
{
    public class EdgeTapPage : Page
    {
        public delegate void CornerTapEvent(RectLineBoundary location);
        public delegate void EdgeTapEvent(RectLineBoundary location);

        public static event EdgeTapEvent EdgeTap;
        public static event CornerTapEvent CornerTap;

        // The fraction of the screen width/height edge taps that will invoke sidebars 
        const double fraction = 1 / 20.0;


        public EdgeTapPage()
        {
            ListenForClick();
        }

        private void ListenForClick()
        {
            Tapped += (object sender, TappedRoutedEventArgs e) =>
            {
                Point point = e.GetPosition(this);

                //// The fraction of the screen width/height edge taps that will invoke sidebars 
                //double fraction = 1 / 20.0;

                //Boolean isTapInFirstWidth = point.X < fraction * this.ActualWidth;
                //Boolean isTapInFirstHeight = point.Y < fraction * this.ActualHeight;

                //Boolean isTapInLastWidth = point.X > (1 - fraction) * this.ActualWidth;
                //Boolean isTapInLastHeight = point.Y > (1 - fraction) * this.ActualHeight;


                NotifyListenersIfEdgeTap(e, point.X, point.Y);
            };
        }

        private void NotifyListenersIfEdgeTap(TappedRoutedEventArgs e, double x, double y)
        {
            RectLineBoundary? clickEdgeLocationMaybe = getEdgeLocation(x, y);

            //log.Info("tap (x:{0}, y:{1}) = {2}", point.X, point.Y, clickEdgeLocation);

            if (EdgeTap != null && clickEdgeLocationMaybe != null)
            {
                EdgeTap(clickEdgeLocationMaybe.Value);

                e.Handled = true;
            }
        }

        private RectJointBoundary? getCornerLocation(double x, double y)
        {
            bool isTapInFirstWidth = x < fraction * this.ActualWidth;
            bool isTapInLastWidth = x > (1 - fraction) * this.ActualWidth;

            bool isTapInFirstHeight = y < fraction * this.ActualHeight;
            bool isTapInLastHeight = y > (1 - fraction) * this.ActualHeight;

            //log.Info("tap (x:{0}, y:{1}", x, y);

            //EdgeLocation clickEdgeLocation = getEdgeLocation(x, y);

            // Handle most specific cases = corners
            if (isTapInFirstHeight && isTapInFirstWidth)
                return RectJointBoundary.TopLeft;
            if (isTapInFirstHeight && isTapInLastWidth)
                return RectJointBoundary.TopRight;
            if (isTapInLastHeight && isTapInLastWidth)
                return RectJointBoundary.BottomRight;
            if (isTapInLastHeight && isTapInFirstWidth)
                return RectJointBoundary.BottomLeft;

            return null;
        }

        private RectLineBoundary? getEdgeLocation(double x, double y)
        {
            bool isTapInFirstWidth = x < fraction * this.ActualWidth;
            bool isTapInLastWidth = x > (1 - fraction) * this.ActualWidth;

            bool isTapInFirstHeight = y < fraction * this.ActualHeight;
            bool isTapInLastHeight = y > (1 - fraction) * this.ActualHeight;

            //log.Info("tap (x:{0}, y:{1}", x, y);

            //EdgeLocation clickEdgeLocation = getEdgeLocation(x, y);

            // Handle non-corners
            if (isTapInFirstWidth)
                return RectLineBoundary.Left;
            if (isTapInLastWidth)
                return RectLineBoundary.Right;
            if (isTapInFirstHeight)
                return RectLineBoundary.Top;
            if (isTapInLastHeight)
                return RectLineBoundary.Bottom;

            return null;
        }


    }
}
