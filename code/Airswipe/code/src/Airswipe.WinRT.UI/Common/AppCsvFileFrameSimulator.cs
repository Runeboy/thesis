using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.NatNetPortable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Airswipe.WinRT.UI.Common
{
    class AppCsvFileFrameSimulator
    {
        #region Fields

        private static readonly ILogger log = new TypeLogger<AppCsvFileFrameSimulator>();

        public static AppCsvFileFrameSimulator Instance = new AppCsvFileFrameSimulator();

        //public readonly string Filepath;

        private Boolean isContinuous;

        public event EventHandler End;

        private CancellationTokenSource source;

        //public event FrameReady FrameReady;

        #endregion
        #region Constructors

        private AppCsvFileFrameSimulator() //string filepath)
        {
            //Filepath = filepath;
        }

        #endregion
        #region Methods

        public async void Start(string filepath)
        {
            //IsEnabled = true;
            if (source != null)
                source.Cancel();

            source = new CancellationTokenSource();

            Task.Run(
                () => ParseCsv(filepath, source.Token, End)
                );
        }

        public void Stop()
        {
            source.Cancel();
        }

        private async void ParseCsv(string filepath, CancellationToken cancellationToken, EventHandler end)
        {
            //Filepath = filepath;

            log.Info("Parsing file: " + filepath);

            IStorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filepath);
            if (file == null)
                throw new ArgumentException("File '" + filepath + "' does not exist");

            //IList<string> lines = await FileIO.ReadLinesAsync(file);

            ParseCsvRecursiveMaybe(filepath, cancellationToken, end);
        }

        private void ParseCsvRecursiveMaybe(string filepath, CancellationToken cancellationToken, EventHandler end)
        {
            var parser = new NatNetFrameCsvParser();
            //parser.ParseEnded += (sender, args) =>
            //{
            //};

            DateTime startTime = DateTime.Now;

            int count = 0;

            parser.FrameParsed += async (frame, timeOffsetSeconds, isLastFrame) =>
            {
                DateTime targetTime = startTime.AddSeconds(timeOffsetSeconds);
                int millisecondsToWait = (int)(targetTime - DateTime.Now).TotalMilliseconds;
                if (millisecondsToWait > 0)
                    await Task.Delay(millisecondsToWait);

                if (source.IsCancellationRequested)
                    return;

                AppMotionTrackerClient.Instance.SimulateFrameReceival(frame);
                count++;

                if (isLastFrame)
                {
                    log.Info("Last frame parse event, {0} received", count);

                    if (IsContinuous)
                        ParseCsvRecursiveMaybe(filepath, cancellationToken, end);
                    else
                        if (end != null)
                            end(null, null);
                }
            };

            parser.ParseCsv(filepath, cancellationToken);
        }

        #endregion
        #region Properties

        public bool IsContinuous { get; set; }

        #endregion
    }
}