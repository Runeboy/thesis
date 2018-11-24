using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.MotionTracking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Airswipe.WinRT.NatNetPortable
{
    public class NatNetFrameCsvParser
    {
        #region Fields

        public delegate void FrameParse(FrameOfMocapData frame, double timeOffsetSeconds, bool isLastFrame);

        public event FrameParse FrameParsed;

        //public EventHandler ParseEnded;

        private static readonly ILogger log = new TypeLogger<NatNetFrameCsvParser>();

        #endregion
        #region Methods

        public async void ParseCsv(string filepath, CancellationToken cancellationToken)
        {
            IStorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filepath);
            if (file == null)
                throw new ArgumentException("File '" + filepath + "' does not exist");

            NatNetML.FrameOfMocapData frame = new NatNetML.FrameOfMocapData();

            int lineNumber = 0;
            int FrameParseEventsSent = 0;
            using (Stream fileStream = await file.OpenStreamForReadAsync())
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string[] itemTypeLineTokens = null;
                string[] itemNameTokens = null;

                while (reader.Peek() >= 0 && !cancellationToken.IsCancellationRequested)
                {
                    string line = reader.ReadLine();
                    lineNumber++;
                    string[] tokens = line.Split(',');

                    bool isHeaderData = lineNumber <= 7;

                    //List<int> ridigBodyIndexes = new List<int>();
                    //List<int> ridigBodyMarkerIndexes = new List<int>();
                    //List<int> markerIndexes = new List<int>();


                    const int ItemTypesLineNumer = 3;
                    if (lineNumber == ItemTypesLineNumer)
                        itemTypeLineTokens = tokens;

                    const int ItemNamesLineNumer = 4;
                    itemNameTokens = tokens;

                    if (!isHeaderData)
                    {
                        //NatNetML.FrameOfMocapData frame = new NatNetML.FrameOfMocapData();
                        frame.nMarkers = 0;
                        frame.nRigidBodies = 0;
                        frame.nMarkerSets = 0;
                        frame.nOtherMarkers = 0;
                        frame.nSkeletons = 0;

                        int frameIndex = Int32.Parse(tokens[0]);
                        double timeOffset = Double.Parse(tokens[1]);

                        string lastItemType = null;
                        for (int tokenIndex = 2; tokenIndex < tokens.Length; tokenIndex++) // frame data starts from cell number 3 / index 2
                        {
                            string token = tokens[tokenIndex];
                            string itemType = itemTypeLineTokens[tokenIndex];
                            //Boolean isNewItem = (itemType != lastItemType);

                            //if (isNewItem)
                            //{
                            if (itemType == "Rigid Body")
                            {
                                NatNetML.RigidBodyData r = frame.RigidBodies[frame.nRigidBodies];
                                r.nMarkers = 0;
                                r.ID = frame.nRigidBodies + 1;
                                r.qx = float.Parse(tokens[tokenIndex++]); // munch a number of tokens, these are hardcoded based on expected csv format
                                r.qy = float.Parse(tokens[tokenIndex++]);
                                r.qz = float.Parse(tokens[tokenIndex++]);
                                r.qw = float.Parse(tokens[tokenIndex++]);
                                r.x = float.Parse(tokens[tokenIndex++]);
                                r.y = float.Parse(tokens[tokenIndex++]);
                                r.z = float.Parse(tokens[tokenIndex++]);
                                r.MeanError = float.Parse(tokens[tokenIndex]);

                                frame.nRigidBodies++;
                            }
                            else if (itemType == "Rigid Body Marker")
                            {
                                NatNetML.RigidBodyData r = frame.RigidBodies[frame.nRigidBodies - 1]; // retrieve previously set rigid body
                                NatNetML.Marker m = r.Markers[r.nMarkers];
                                m.ID = r.nMarkers + 1; //Int32.Parse(itemNameTokens[tokenIndex]);
                                m.x = float.Parse(tokens[tokenIndex++]); // munch 4 tokens, these are hardcoded based on expected csv format
                                m.y = float.Parse(tokens[tokenIndex++]);
                                m.z = float.Parse(tokens[tokenIndex]);
                                tokenIndex++; // skip quality token

                                r.nMarkers++;
                            }
                            else if (itemType == "Marker")
                            {
                                NatNetML.Marker m = frame.OtherMarkers[frame.nOtherMarkers];
                                m.ID = frame.nOtherMarkers + 1; //Int32.Parse(itemNameTokens[tokenIndex]);
                                m.x = float.Parse(tokens[tokenIndex++]); // munch 3 tokens, these are hardcoded based on expected csv format
                                m.y = float.Parse(tokens[tokenIndex++]);
                                m.z = float.Parse(tokens[tokenIndex]);

                                frame.nOtherMarkers++;
                            }

                            lastItemType = itemType;
                            //}

                        }

                        //Debug.WriteLine(lineNumber);
                        if (FrameParsed != null) { 
                            FrameParsed(NatNetFrameOfMocapData.Create(frame), timeOffset, reader.Peek() < 0);
                            FrameParseEventsSent++;
                        }
                    }
                }
            }

            //if (ParseEnded != null)
            //    ParseEnded(this, null);

            log.Info("Parsed {0} lines, sent {1} frame parse events", lineNumber, FrameParseEventsSent);
        }

        #endregion
    }
}
