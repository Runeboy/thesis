using Airswipe.WinRT.Core.MotionTracking;
using Airswipe.WinRT.NatNetPortable;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AirSwipe.WinRT.NatNetPortable
{
    public static class NatNetSerializer
    {
        #region Fields

        private static JsonSerializer serializer = new JsonSerializer();

        #endregion
        #region Methods


        public async static Task<FrameOfMocapData> DeserializeFrameJsonFileFromAppLocalFolderAsync(string fileContents)
        {
            var frame = JsonConvert.DeserializeObject<NatNetML.FrameOfMocapData>(fileContents);

            return NatNetFrameOfMocapData.Create(frame);
        }

        public async static Task<FrameOfMocapData> DeserializeFrameJsonFileFromAppLocalFolderAsync(Stream fileStream)
        {
            using (StreamReader streamReader = new StreamReader(fileStream))
            using (JsonReader jsonReader = new JsonTextReader(streamReader))
            {
                // json size doesn't matter because only a small piece is read at a time 
                var frame = serializer.Deserialize<NatNetML.FrameOfMocapData>(jsonReader);
                return NatNetFrameOfMocapData.Create(frame);
             //   return serializer.Deserialize<Skod>(jsonReader);
            }
        }

        #endregion
    }
}
