using Airswipe.WinRT.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Airswipe.WinRT.Core.MotionTracking
{
    public class MotionClientStatistics
    {
        #region Fields

        private static readonly ILogger log = new TypeLogger<MotionClientStatistics>();

        private DispatcherTimer timer;

        private long lastFrameCount;
        private DateTime lastCheckTime = DateTime.Now;

        public delegate void StatisticsUpdated(double framesPerSecond);

        public event StatisticsUpdated Updated;

        private Queue<double> framesPerSecondQueue = new Queue<double>();

        private const int FRAME_QUEUE_MAX_SIZE = 120; // Assume circa 120 FPS

        private readonly SimplifiedMotionTrackerClient client;

        #endregion
        #region Constructors

        public MotionClientStatistics(SimplifiedMotionTrackerClient _client, int updateIntervalMilliseconds)
        {
            log.Verbose("construct");

            client = _client;
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(updateIntervalMilliseconds) };

            UpdateFrameRateUsingTimer(client);
        }

        #endregion
        #region Methods

        private void UpdateFrameRateUsingTimer(SimplifiedMotionTrackerClient client)
        {
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            long currentFrameCount = client.FrameCount;
            long framesSinceLastTick = currentFrameCount - lastFrameCount;
            lastFrameCount = currentFrameCount;

            var now = DateTime.Now;
            double secondsPassed = (now - lastCheckTime).TotalSeconds;
            lastCheckTime = now;

            double framesPerSecond = framesSinceLastTick / secondsPassed;


            double meanFps;
            lock (framesPerSecondQueue)
            {
                if (framesPerSecondQueue.Count > 0)
                    framesPerSecondQueue.Dequeue();

                framesPerSecondQueue.Enqueue(framesPerSecond);

                meanFps = framesPerSecondQueue.Sum() / framesPerSecondQueue.Count;
            }

            if (Updated != null)
                Updated(meanFps);
        }

        #endregion
        #region Properties

        public double FramesPerSecond
        {
            get
            {
                lock (framesPerSecondQueue)
                {
                    return framesPerSecondQueue.Sum() / framesPerSecondQueue.Count;
                }
            }
        }

        #endregion
    }
}
