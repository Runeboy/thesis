using Airswipe.WinRT.Core.Data;
using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.MotionTracking;
using AirSwipe.WinRT.NatNetPortable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Airswipe.WinRT.UI.Common
{
    public class AppFrameFileReader
    {
        #region Fields

        private static readonly ILogger log = new TypeLogger<AppFrameFileReader>();

        public static readonly AppFrameFileReader Instance = new AppFrameFileReader();

        //private const string FrameFilename = "frameFile.json";
        private static readonly StorageFolder frameFileLocation = ApplicationData.Current.LocalFolder;
        private DateTimeOffset lastDateModified = DateTimeOffset.MinValue;

        // private DispatcherTimer FrameFileDeserializePollTimer = new DispatcherTimer();
        private DispatcherTimer FrameFileDeserializePollTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100) };

        private const int expectedMillisecondsBetweenFrames = 10000 / 120;

        //        private DispatcherTimer FileReadTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(expectedMillisecondsBetweenFrames) };

        public event FrameReady FrameReady;

        //private int lastFrameId = -1;
        private int lastFrameNumber = -1;

        private bool isEnabled;

        private static readonly DateTime StartupTime = DateTime.Now;

        private const int MAX_FILE_AGE_SECONDS = 7;

        #endregion
        #region Constructors

        private AppFrameFileReader()
        {
            log.Info("construct");

            FrameFileDeserializePollTimer.Tick += FrameFileDeserializePollTimer_Tick;
        }

        private static StorageFileQueryResult frameFilesQuery;
        IReadOnlyList<StorageFile> files;

        //async void ListenForFrameFilesChange()
        //{
        //    frameFilesQuery = CreateFrameFilesQuery();
        //    frameFilesQuery.ContentsChanged += QueryContentsChanged;
        //    files = await frameFilesQuery.GetFilesAsync();
        //}

        private static StorageFileQueryResult CreateFrameFilesQuery()
        {
            StorageFileQueryResult frameFilesQuery;

            var options = new Windows.Storage.Search.QueryOptions
            {
                FileTypeFilter = { ".json" },
                FolderDepth = Windows.Storage.Search.FolderDepth.Shallow,
            };

            frameFilesQuery = ApplicationData.Current.LocalFolder.CreateFileQueryWithOptions(options);
            return frameFilesQuery;
        }

        private async Task<List<StorageFile>> GetFrameFiles()
        {
            return (await CreateFrameFilesQuery().GetFilesAsync()).Where(
                file => IsInteger(file.DisplayName)
                ).ToList();
        }

        private static bool IsInteger(String str)
        {
            return Regex.IsMatch(str, @"^\d+$");
        }

        //private static long GetTimestamp()
        //{
        //    var timeSpan = (DateTime.Now - DateTime.MinValue);
        //    return (long)timeSpan.TotalMilliseconds;
        //}

        //private const long DelayMilliseconds = 10000;


        //Queue<FrameOfMocapData> frameQueue = new Queue<FrameOfMocapData>();

        async void QueryContentsChanged(IStorageQueryResultBase sender, object args)
        {
            frameFilesQuery.ContentsChanged -= QueryContentsChanged;

            //List<StorageFile> files = (await GetFrameFiles()).OrderBy(
            //    f => f.DisplayName
            //    ).ToList();

            files = files.OrderBy(
                f => f.DisplayName
                ).ToList();

            //Debug.WriteLine(StringExpert.CommaSeparate(files.Select(f => f.DisplayName)));

            foreach (var file in files)
            {
                if (file.DateCreated < StartupTime)
                    continue;
                //Debug.WriteLine("X");

                int frameNumber = int.Parse(file.DisplayName);
                //long now = GetTimestamp();

                if (frameNumber <= lastFrameNumber)
                {
                    //log.Verbose("Ignoring past file {0} ({1}) ", timestamp, DateTime.MinValue + TimeSpan.FromMilliseconds(timestamp));
                    continue;
                }

                lastFrameNumber = frameNumber;

                //long elapsedMilliseconds = now - timestamp;

                //if (elapsedMilliseconds > DelayMilliseconds)
                //{
                //    //log.Verbose("Skipping frame timestamp {0} as its age {1} it is older than {2} ms", now, elapsedMilliseconds, DelayMilliseconds);
                //    continue;
                //}

                //new DateTimeOffset.UtcNow()

                var fileAge = DateTime.Now - file.DateCreated;
                if (fileAge.TotalSeconds > MAX_FILE_AGE_SECONDS)
                    continue;

                //var t = await FileIO.ReadTextAsync(file);
                //var frame = await NatNetSerializer.DeserializeFrameJsonFileFromAppLocalFolderAsync(t);


                FrameOfMocapData frame;
                using (Stream fileStream = await file.OpenStreamForReadAsync())
                {
                    frame = await NatNetSerializer.DeserializeFrameJsonFileFromAppLocalFolderAsync(fileStream);
                }
                if (FrameReady != null)
                    FrameReady(frame, null);


                //Debug.WriteLine("--" + file.DisplayName);

                file.DeleteAsync();
            }


            frameFilesQuery.ContentsChanged += QueryContentsChanged;
            files = await frameFilesQuery.GetFilesAsync();
        }

        //public bool IsPollIntervalAutoAdjusted = true;
        //public int AutoAdjustIntervalMilliSeconds = 10;

        //DateTime lastTick = DateTime.MinValue;
        //Queue<FrameOfMocapData> frameQueue = new Queue<FrameOfMocapData>();

        //private static DateTime TimestampToDateTime(long timestamp)
        //{
        //    return DateTime.MinValue + TimeSpan.FromMilliseconds(timestamp);
        //}

        private async void FrameFileDeserializePollTimer_Tick(object sender, object e)
        {
            (sender as DispatcherTimer).Stop();

            List<StorageFile> files = (await GetFrameFiles()).OrderBy(
                f => f.DisplayName
                ).ToList();

            //Debug.WriteLine(StringExpert.CommaSeparate(files.Select(f => f.DisplayName)));

            foreach (var file in files)
            {
                if (file.DateCreated < StartupTime)
                    continue;
                //Debug.WriteLine("X");

                int frameNumber = int.Parse(file.DisplayName);
                //long now = GetTimestamp();

                if (frameNumber <= lastFrameNumber)
                {
                    //log.Verbose("Ignoring past file {0} ({1}) ", timestamp, DateTime.MinValue + TimeSpan.FromMilliseconds(timestamp));
                    continue;
                }

                lastFrameNumber = frameNumber;

                //long elapsedMilliseconds = now - timestamp;

                //if (elapsedMilliseconds > DelayMilliseconds)
                //{
                //    //log.Verbose("Skipping frame timestamp {0} as its age {1} it is older than {2} ms", now, elapsedMilliseconds, DelayMilliseconds);
                //    continue;
                //}

                //new DateTimeOffset.UtcNow()

                var fileAge = DateTime.Now - file.DateCreated;
                if (fileAge.TotalSeconds > MAX_FILE_AGE_SECONDS)
                    continue;

                //var t = await FileIO.ReadTextAsync(file);
                //var frame = await NatNetSerializer.DeserializeFrameJsonFileFromAppLocalFolderAsync(t);

                FrameOfMocapData frame;
                using (Stream fileStream = await file.OpenStreamForReadAsync())
                {
                    frame = await NatNetSerializer.DeserializeFrameJsonFileFromAppLocalFolderAsync(fileStream);
                }
                if (FrameReady != null)
                    FrameReady(frame, null);


                //Debug.WriteLine("--" + file.DisplayName);

                file.DeleteAsync();
            }


            if (IsEnabled)
                (sender as DispatcherTimer).Start();
        }
        //    //var now = DateTime.Now;

        //    //if (lastTick == DateTime.MinValue)
        //    //{
        //    //    lastTick = now;
        //    //    return;
        //    //}

        //    //var timeSinceLastTick = now - lastTick;
        //    //lastTick = now;




        //    //---------------

        //    List<StorageFile> files = await GetFrameFiles();

        //    if (files.Count < 2)
        //    {
        //        log.Info("Frame file count less than 2, ignoring..");
        //        return;
        //    }

        //    var mostRecentFirst = files.OrderByDescending(f => f.DisplayName);
        //    var file = mostRecentFirst.Skip(1).First(); // take the second most recent to avoid contending for a file resource with the frame writer

        //    int frameNumber = Int32.Parse(file.DisplayName);
        //    if (frameNumber == lastFrameNumber)
        //    {
        //        log.Verbose("Frame number equals the last processed ({0}), skipping..", lastFrameNumber);
        //        //if (IsPollIntervalAutoAdjusted)
        //        //    PollIntervalMilliSeconds += AutoAdjustIntervalMilliSeconds;
        //        return;
        //    }

        //    if (frameNumber - lastFrameNumber > 1)
        //    {
        //        //if (IsPollIntervalAutoAdjusted)
        //        //    PollIntervalMilliSeconds -= AutoAdjustIntervalMilliSeconds;
        //    }

        //    lastFrameNumber = frameNumber;

        //    FrameOfMocapData frame;
        //    using (Stream fileStream = await (file as StorageFile).OpenStreamForReadAsync())
        //    {
        //        frame = await NatNetSerializer.DeserializeFrameJsonFileFromAppLocalFolderAsync(fileStream);
        //    }
        //    log.Verbose("deserialized next frame (id {0})", frame.iFrame);
        //    if (FrameReady != null)
        //        FrameReady(frame, null);
        //}

        #endregion
        #region Methods

        //public async void StartPolling(int intervalMilliSeconds)
        //{
        //    log.Info("Start polling for frame files");

        //    IsEnabled = true;

        //    ProcessFrameFilesUntilNotEnabled();

        //    //bool fileExists = await frameFileLocation.TryGetItemAsync(FrameFilename) != null;
        //    //if (!fileExists) { 
        //    //    log.Error(@"Cannot start polling as frame file does not exist ({0}\{1})", frameFileLocation.Path, FrameFilename);
        //    //    return;
        //    //}

        //    //PollIntervalMilliSeconds = intervalMilliSeconds;
        //    //FrameFileDeserializePollTimer.Start();
        //    //ListenForFrameFilesChange();
        //    //FrameFileDeserializePollTimer.Start();
        //    //ListenForFrameFilesChange();

        //    //FileReadTimer.Tick += FileReadTimer_Tick;
        //    //FileReadTimer.Start();
        //}

        //async void FileReadTimer_Tick(object sender, object e)
        //{
        //    //await CheckForFrameFiles();

        //}

        private async void Skod()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("frame.json");
            if (file == null) // file.DateCreated < StartupTime)
                return;

            while (IsEnabled)
            {

                try
                {
                    //StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("frame.json");
                    //if (file.DateCreated < StartupTime)
                    //    return;

                    string s = await FileIO.ReadTextAsync(file);

                    //FrameOfMocapData frame;
                    //using (Stream fileStream = await file.OpenStreamForReadAsync())
                    //{
                    //    frame = await NatNetSerializer.DeserializeFrameJsonFileFromAppLocalFolderAsync(fileStream);
                    //}
                    //if (FrameReady != null)
                    //    FrameReady(frame, null);
                }
                catch (Exception e)
                {
                    log.Error("Could not read file {0} ({1})", file.Path, e.Message);
                }
            }
        }

        private async Task ProcessFrameFilesUntilNotEnabled()
        {
            //        StorageFile file = (await GetFrameFiles()).OrderBy(
            //f => f.DisplayName
            //).Last();

            while (IsEnabled)
            {
                //                if (file == null) continue;
                //int skod = 0;

                //while (file == null) {
                //    skod++;
                //    file = await ApplicationData.Current.LocalFolder.GetFileAsync((lastFrameNumber + skod) + ".json");
                //    Debug.WriteLine((lastFrameNumber + skod) + ".json");
                //}

                //   log.Info("Tick at {0}", DateTime.Now);

                // handle changes

                List<StorageFile> files = (await GetFrameFiles()).OrderBy(
                    f => f.DisplayName
                    ).ToList();




                //if (files.Count < 2)
                //{
                //    log.Info("Frame file count less than 2, ignoring..");
                //    return;
                //}

                //sender.ContentsChanged -= QueryContentsChanged;


                foreach (var file in files)
                {
                    if (file.DateCreated < StartupTime)
                        continue;
                    //Debug.WriteLine("X");

                    int frameNumber = int.Parse(file.DisplayName);
                    //long now = GetTimestamp();

                    if (frameNumber <= lastFrameNumber)
                    {
                        //log.Verbose("Ignoring past file {0} ({1}) ", timestamp, DateTime.MinValue + TimeSpan.FromMilliseconds(timestamp));
                        continue;
                    }

                    lastFrameNumber = frameNumber;

                    //long elapsedMilliseconds = now - timestamp;

                    //if (elapsedMilliseconds > DelayMilliseconds)
                    //{
                    //    //log.Verbose("Skipping frame timestamp {0} as its age {1} it is older than {2} ms", now, elapsedMilliseconds, DelayMilliseconds);
                    //    continue;
                    //}

                    //new DateTimeOffset.UtcNow()

                    var fileAge = DateTime.Now - file.DateCreated;
                    if (fileAge.TotalSeconds > MAX_FILE_AGE_SECONDS)
                        continue;

                    //var t = await FileIO.ReadTextAsync(file);
                    //var frame = await NatNetSerializer.DeserializeFrameJsonFileFromAppLocalFolderAsync(t);


                    FrameOfMocapData frame;
                    using (Stream fileStream = await file.OpenStreamForReadAsync())
                    {
                        frame = await NatNetSerializer.DeserializeFrameJsonFileFromAppLocalFolderAsync(fileStream);
                    }
                    if (FrameReady != null)
                        FrameReady(frame, null);


                    //Debug.WriteLine("--" + file.DisplayName);

                    file.DeleteAsync();
                }
                //                file = null;

                //if ((await  file.GetBasicPropertiesAsync).)


                //Debug.WriteLine(frameNumber);
                //Debug.WriteLine(frameNumber);



                //if (1 == 1) continue;

                //long millisecondsToProcessFrame = DelayMilliseconds - elapsedMilliseconds;


                //Task.Run(
                //    async () =>
                //    {
                //        await Task.Delay((int)millisecondsToProcessFrame);
                //        //Debug.WriteLine("processing " + file.DisplayName);
                //        log.Verbose("deserialized next frame (id {0})", frame.iFrame);
                //        if (FrameReady != null)
                //            FrameReady(frame, null);
                //    }
                //    );

                //}
            }
        }

        //public void StopPolling()
        //{
        //    //FrameFileDeserializePollTimer.Stop();

        //    log.Info("Stopped polling for frame files");
        //}

        #endregion
        #region Properties

        //public AppFrameFileReader Instance
        //{
        //    get
        //    {
        //        if ()
        //    }
        //}

        //public double PollIntervalMilliSeconds
        //{
        //    get { return FrameFileDeserializePollTimer.Interval.TotalMilliseconds; }
        //    set
        //    {
        //        FrameFileDeserializePollTimer.Interval = TimeSpan.FromMilliseconds(value);

        //        log.Verbose("Poll interval adjusted to {0} ms", PollIntervalMilliSeconds);
        //    }
        //}

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                isEnabled = value;

                if (IsEnabled)
                    FrameFileDeserializePollTimer.Start();
                //    ListenForFrameFilesChange();
                else
                    FrameFileDeserializePollTimer.Stop();
                //    frameFilesQuery = null;

                //ProcessFrameFilesUntilNotEnabled();
                //Skod();

                log.Info("Status: {0}", isEnabled ? "ENABLED" : "DISABLED");

            }
        }

        #endregion
    }
}
