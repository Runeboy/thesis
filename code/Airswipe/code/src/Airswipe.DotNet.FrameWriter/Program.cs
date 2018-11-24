using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Airswipe.DotNet.FrameWriter
{
    /// <summary>
    /// This class listens for motion tracking broadcasts using the NatNatML library and persists/serializes any received data frames to a file using JSON.net (which outperforms native c# serialization methods). 
    /// </summary>
    class Program
    {
        #region Fields

        private static string framesDirPath;

        private static Boolean IsFrameWriteEnabled = true;

        //const int MAX_FRAME_FILE_COUNT = 25;
        const int MAX_AGE_MILLISECONDS = 10000;

        const string TempFilename = "current.temp";

        static JsonSerializer serializer = new JsonSerializer();

        static Timer deleteOldFilesTimer = new Timer() { Interval = 1000 }; // every second
        static Timer printFrameRateTimer = new Timer() { Interval = 1000 }; // every second

        static int frameCount = 0;

        //static string frameFilePath;

        //bool isWritingFrame = false;

        //static Object fileIoLock = new Object();

        //static string strLocalIP =
        //    //"192.168.43.56";
        //    //        string strLocalIP = "10.0.0.17";
        //    "169.254.214.82";

        #endregion
        #region Methods

        static void Main(string[] args)
        {
            DeleteOldFilesFrequently();

            PrintFrameRateFrequently();

            framesDirPath = ConfigurationManager.AppSettings["FrameFilePath"];
            if (String.IsNullOrEmpty(framesDirPath))
                throw new Exception("Frame filepath is not configured");

            var dirInfo = new DirectoryInfo(framesDirPath);
            if (!dirInfo.Exists)
                throw new Exception(String.Format("Frames directory '{0}' does not exist.", dirInfo));

            Console.WriteLine("Writing recieved frames to path: " + framesDirPath);

            //frameFilePath = String.Format("{0}\\{1}.json", framesDirPath, "frame");//, DateTimeOffset.Now.UtcTicks);
            //File.Create(frameFilePath).Close();

            var client = new NatNetML.NatNetClientML(0); // 0 = Multicast

            InitializeClient(client);

            EnsureServerDescriptionAvailable(client);
            EnsureDataDescriptionAvailable(client);

            ListenForFramesReceived(client);

            ListenAndRespondToInputCharacters();

            Console.WriteLine("\n\nQuitting..");
        }

        private static void PrintFrameRateFrequently()
        {
            DateTime last = DateTime.Now;
            printFrameRateTimer.Elapsed += (sender, args) =>
            {
                if (frameCount > 0)
                {
                    double elapsed = (DateTime.Now - last).TotalSeconds;
                    double frameRate = frameCount / elapsed;
                    Console.Write("FPS:" + (int)frameRate + "\t");
                    
                    frameCount = 0; // ignore race conditions;
                    last = DateTime.Now;
                }
            };
            printFrameRateTimer.Start();
        }

        private static void DeleteOldFilesFrequently()
        {
            deleteOldFilesTimer.Elapsed += (sender, args) =>
            {
                DeleteOldestFrameFiles();
            };
            deleteOldFilesTimer.Start();
        }

        private static void InitializeClient(NatNetML.NatNetClientML client)
        {
            Console.WriteLine("Initializing client..");


            string strLocalIP = GetLocalIpAddress();

            Console.WriteLine("Local IP is: " + strLocalIP);

            string strServerIP = strLocalIP;

            bool success = client.Initialize(strLocalIP, strServerIP) == 0;
            if (!success)
                throw new Exception("error initializing");
        }

        private static void ListenForFramesReceived(NatNetML.NatNetClientML client)
        {
            client.OnFrameReady += new NatNetML.FrameReadyEventHandler(m_NatNet_OnFrameReady);
        }

        private static bool EnsureDataDescriptionAvailable(NatNetML.NatNetClientML client)
        {
            List<NatNetML.DataDescriptor> dataDescriptors = new List<NatNetML.DataDescriptor>();
            bool success = client.GetDataDescriptions(out dataDescriptors);
            if (!success)
                throw new Exception("error retrieving data descriptions");
            return success;
        }

        private static void EnsureServerDescriptionAvailable(NatNetML.NatNetClientML client)
        {
            var desc = new NatNetML.ServerDescription();
            bool success = (client.GetServerDescription(desc) == 0);
            if (!success)
                throw new Exception("error retrieving server description");
        }

        private static void ListenAndRespondToInputCharacters()
        {
            char commandChar;
            Console.WriteLine("\nWaiting for command (q = quit, w = write toggle)..");

            while ((commandChar = Console.ReadKey().KeyChar) != 'q')
            {
                if (commandChar == 'w')
                {
                    IsFrameWriteEnabled = !IsFrameWriteEnabled;
                    Console.Write("\n\nWrite is now {0}\n\n", IsFrameWriteEnabled ? "ENABLED" : "DISABLED");
                }
            }
        }

        private static void DeleteOldestFrameFiles()
        {
            string[] filepaths = Directory.GetFiles(framesDirPath, "*.json");

            //for (int fileNumber = filepaths.Length; fileNumber > MAX_FRAME_FILE_COUNT; fileNumber--)
            //{
            //    FileInfo oldest = null;
            //    foreach (string filepath in filepaths)
            //    {
            //        FileInfo fileInfo = new FileInfo(filepath);
            //        if (oldest == null || fileInfo.CreationTime < oldest.CreationTime)
            //            oldest = fileInfo;
            //    }

            //    File.Delete(oldest.FullName);
            //}


            foreach (string filepath in filepaths)
            {
                FileInfo fileInfo = new FileInfo(filepath);

                //long timestamp = long.Parse(Path.GetFileNameWithoutExtension(filepath));
                //long now = GetTimestamp();
                //                long ageMilliseconds = now - timestamp;
                double ageMilliseconds = (DateTime.Now - fileInfo.CreationTime).TotalMilliseconds;
                if (ageMilliseconds > MAX_AGE_MILLISECONDS)
                    //lock (fileIoLock)
                    File.Delete(filepath);
            }
        }

        private static void m_NatNet_OnFrameReady(NatNetML.FrameOfMocapData frame, NatNetML.NatNetClientML client)
        {
            frameCount++; // ignore race conditions
            WriteFrameToStorageAsync(frame);
        }

        private static async void WriteFrameToStorageAsync(NatNetML.FrameOfMocapData frame)
        {
            TrimUnusedFrameArrayParts(frame);

            //Console.Write("{0} >", frame.iFrame);

            if (frame.iFrame % 10 == 0)
                Console.Write(frame.iFrame.ToString() + "\t");

            if (!IsFrameWriteEnabled)
            {
                //Console.Write(" ");
                return;
            }


            string frameFilePath = String.Format("{0}\\{1}.json", framesDirPath, frame.iFrame);//, DateTimeOffset.Now.UtcTicks);

            File.Delete(frameFilePath);

            // use the serializer directly to avoid having the string in memory and allow the (likely) concurrent sharing of the contents
            //using (FileStream fs = File.Open(TempFilename, FileMode.Create, FileAccess.Write, FileShare.Read))
            //File.Create(frameFilePath).Close;

            try { 
                using (FileStream fs = File.Open(frameFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (StreamWriter sw = new StreamWriter(fs))
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    jw.Formatting = Formatting.Indented;
                    serializer.Serialize(jw, frame);
                }
                }
            catch(Exception e)
            {
                Console.Write("\n" + e.Message + "\n");
            }
            //File.Copy(TempFilename, frameFilePath);
        }

        public static long GetTimestamp()
        {
            var timeSpan = (DateTime.Now - DateTime.MinValue);
            return (long)timeSpan.TotalMilliseconds;
        }

        private static void TrimUnusedFrameArrayParts(NatNetML.FrameOfMocapData frame)
        {
            Array.Resize(ref frame.MarkerSets, frame.nMarkerSets);
            foreach (var markerSet in frame.MarkerSets)
                Array.Resize(ref markerSet.Markers, markerSet.nMarkers);
            //TrimMarkerSetData(ref markerSet);


            Array.Resize(ref frame.LabeledMarkers, frame.nMarkers);

            Array.Resize(ref frame.OtherMarkers, frame.nOtherMarkers);

            TrimRigidBodies(ref frame.RigidBodies, frame.nRigidBodies);

            Array.Resize(ref frame.Skeletons, frame.nSkeletons);
            foreach (var skeleton in frame.Skeletons)
                TrimRigidBodies(ref skeleton.RigidBodies, skeleton.nRigidBodies);
        }

        private static void TrimRigidBodies(ref NatNetML.RigidBodyData[] rigidBodies, int nRigidBodies)
        {
            Array.Resize(ref rigidBodies, nRigidBodies);
            foreach (var rigidBody in rigidBodies)
                Array.Resize(ref rigidBody.Markers, rigidBody.nMarkers);
        }

        //private static void TrimMarkerSetData(ref NatNetML.MarkerSetData markerSetData)
        //{
        //    Array.Resize(ref markerSetData.Markers, markerSetData.nMarkers);
        //}

        public static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }

            throw new Exception("Could not derive local IP address");
        }

        //private static void TrimMarkerSet(NatNetML.MarkerSetData markerSet)
        //{
        //    Array.Resize(ref markerSet.Markers, markerSet.nMarkers);
        //}

        // [NatNet] [optional] alternate function signatured frame ready callback handler for .NET applications/hosts

        #endregion
    }
}
